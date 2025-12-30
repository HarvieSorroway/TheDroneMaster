using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RWCustom;
using TheDroneMaster.DreamComponent.DreamHook;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster
{
    public class DronePort : PlayerModule.PlayerModuleUtil
    {
        public static float threatMaxDistance = 800f;

        public WeakReference<Player> owner;
        public List<WeakReference<LaserDrone>> drones;
        public DroneState[] states;
        public PortPearlReader pearlReader;

        public IntVector2? lastSpitOutShortcut;
        public WeakReference<Room> lastSpitOutRoom;
        public WeakReference<AbstractCreature> closestDangerCreature = new WeakReference<AbstractCreature>(null);

        public int spawnDroneCoolDown = 40;
        public int preSpawnWaitCounter = 450;
        public int crashedDroneCount = 0;

        public bool usingOverrideSetting = false;

        public int availableDroneCount
        {
            get
            {
                if (Plugin.SkinOnly)
                    return 0;

                int result = 1;
                if (owner.TryGetTarget(out Player player))
                {
                    result = Mathf.CeilToInt((player.Karma + 1f) * Plugin.instance.config.CountPerKarma.Value);

                    if (PlayerPatchs.modules.TryGetValue(player, out var module) && module.stateOverride != null)
                    {
                        result = module.stateOverride.availableDroneCount;
                    }
                }

                return result - crashedDroneCount;
            }
        }

        public bool CanSpawnDrone
        {
            get
            {
                if (Plugin.SkinOnly)
                    return false;

                bool preCondition = preSpawnWaitCounter == 0 && spawnDroneCoolDown <= 0 && drones.Count < availableDroneCount;
                preCondition &= owner.TryGetTarget(out var player) && player.room.regionGate == null;

                return preCondition;
            }
        }

        public DronePort(Player player)
        {
            owner = new WeakReference<Player>(player);
            drones = new List<WeakReference<LaserDrone>>();
            //pearlReader = new PortPearlReader();

            states = new DroneState[availableDroneCount];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new DroneState(this);
            }
            Plugin.Log("This cycle player can get " + availableDroneCount.ToString() + " drones");
        }



        public override void Update(Player player)
        {
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (!drones[i].TryGetTarget(out var drone) || drone.slatedForDeletetion)
                {
                    drones.RemoveAt(i);
                }
            }
            if (spawnDroneCoolDown > 0) spawnDroneCoolDown--;
            if (preSpawnWaitCounter > 0) preSpawnWaitCounter--;

            if (player.inShortcut) return;

            if (InRegionGateOrInShelter(player)) 
                CallBackAllDrones();

            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var drone))
                {
                    if (drone.inShortcut || player.inShortcut) continue;
                    if (drone.room.world.region != player.room.world.region)
                    {
                        drone.Des("Not in same region", false);
                    }
                }
            }

            if (CanSpawnDrone && !player.room.abstractRoom.shelter)
            {
                SpawnNewDrone(player);
            }
            if (drones.Count > availableDroneCount)
            {
                for (int i = drones.Count - availableDroneCount - 1; i >= 0; i--)
                {
                    var reference = drones.Pop();
                    if (reference.TryGetTarget(out var drone))
                    {
                        drone.Des("Too many drones", false);
                    }
                    else continue;
                }
            }
            SearchThreatCreature(player);
        }

        public bool InRegionGateOrInShelter(Player player)
        {
            return (player.room.regionGate != null || player.room.shelterDoor != null);
        }

        public void SearchThreatCreature(Player player)
        {
            float mostDanger = float.MinValue;
            float closestDist = float.MaxValue;
            AbstractCreature mostDangerousCreature = null;
            AbstractCreature closestDangerCreature = null;


            for (int i = 0; i < player.room.abstractRoom.creatures.Count; i++)
            {
                if (player.room.abstractRoom.creatures[i].realizedCreature != null && player.room.abstractRoom.creatures[i].realizedCreature != player)
                {
                    if (player.room.abstractRoom.creatures[i].state == null || !player.room.abstractRoom.creatures[i].state.alive) continue;
                    float danger = DMHelper.ThreatOfCreature(player.room.abstractRoom.creatures[i].realizedCreature, player, false);
                    float distance = Vector2.Distance(player.DangerPos, player.room.abstractRoom.creatures[i].realizedCreature.DangerPos);

                    float threshold = 0.2f;
                    if (danger > threshold)
                    {
                        if (danger > mostDanger && danger > threshold)
                        {
                            mostDangerousCreature = player.room.abstractRoom.creatures[i];
                            mostDanger = danger;
                        }
                        if (distance < closestDist)
                        {
                            closestDangerCreature = player.room.abstractRoom.creatures[i];
                            closestDist = distance;
                        }
                    }
                }
            }

            this.closestDangerCreature.SetTarget(closestDangerCreature);
            if (mostDangerousCreature != null && closestDist <= threatMaxDistance)
            {
                for (int i = drones.Count - 1; i >= 0; i--)
                {
                    if (drones[i].TryGetTarget(out var drone))
                    {
                        drone.AI.ChangeAttackTarget(mostDangerousCreature);
                    }
                }
            }
        }

        public void SpawnNewDrone(Player player)
        {
            spawnDroneCoolDown = 40;

            AbstractCreature abstractDrone = new AbstractCreature(player.room.world, StaticWorld.GetCreatureTemplate(LaserDroneCritob.LaserDrone), null, new WorldCoordinate(player.room.abstractRoom.index, player.coord.x, player.coord.y, -1), player.room.game.GetNewID());
            player.room.abstractRoom.AddEntity(abstractDrone);
            abstractDrone.RealizeInRoom();
            LaserDrone drone = abstractDrone.realizedObject as LaserDrone;
            drone.firstChunk.pos = player.DangerPos;
            drone.port = this;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].ThisStateAvailableForMe(drone))
                {
                    states[i].currentDrone.SetTarget(drone);
                    drone.droneState = states[i];
                    break;
                }
            }

            AddDroneToPort(drone);
        }

        public void CallBackAllDrones()
        {
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone))
                {
                    getDrone.ChangeMovementType(MovementType.CallBack);
                }
            }
        }

        public void ResummonDrones()
        {
            if (!owner.TryGetTarget(out var player)) return;
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone))
                {
                    if (getDrone.room == player.room)
                    {
                        getDrone.ChangeMovementType(MovementType.CallBack);
                    }
                    else
                    {
                        getDrone.Des("Resummon Drones", false);
                    }
                }
            }
        }

        public void ClearOutAllDrones()
        {
            crashedDroneCount += drones.Count;
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone))
                {
                    getDrone.Des("Player Die", true);
                }
            }
        }

        public void AddDroneToPort(LaserDrone drone)
        {
            for (int i = drones.Count - 1; i >= 0; i--)
            {
                if (drones[i].TryGetTarget(out var getDrone) && getDrone == drone)
                {
                    return;
                }
            }
            drones.Add(new WeakReference<LaserDrone>(drone));
            if (PlayerDroneHUD.instance != null)
            {
                PlayerDroneHUD.instance.TryRequestHUDForDrone(drone);
            }
        }
    }
}
