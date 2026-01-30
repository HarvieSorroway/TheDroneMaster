using DMPS.PlayerHooks;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSDrone;
using UnityEngine;
using static TheDroneMaster.DMPS.DMPSPort.DMPSDronePort;

namespace TheDroneMaster.DMPS.DMPSPort
{
    internal class DMPSDronePort : PlayerModule.PlayerModuleUtil
    {
        int counter = 360;
        int noDroneCounter = 0;

        List<DroneSlate> droneSlates = new List<DroneSlate>();

        //List<AbstractDMPSDrone> spawnedDrones = new List<AbstractDMPSDrone>();
        //List<AbstractDMPSDrone> animateDrones = new List<AbstractDMPSDrone>();

        public float armExtend, lastArmExtend;
        public Vector2 armTipPos; //在graphics中计算并赋值
        public Vector2 armTipDir; //在graphics中计算并赋值

        public override void Update(Player player)
        {
            lastArmExtend = armExtend;
            if (player.room == null)
                return;
            if(counter > 0)
            {
                counter--;
                return;
            }
            if(noDroneCounter > 0)
            {
                noDroneCounter--;
            }

            if (droneSlates.Count < 1 && noDroneCounter == 0)
            {
                droneSlates.Add(new DroneSlate(this, player));

                noDroneCounter = 200;
            }

            foreach(var slate in droneSlates)
            {
                slate.UpdateSlate(player);
            }

            ThreatUpdate(player);
            base.Update(player);
        }

        /// <summary> 玩家进入管道时提供更快的寻路目的地 </summary>
        public void EnteringShortcut(IntVector2 intVector2, Player p)
        {
            foreach (var slate in droneSlates)
            {
                slate.OwnerEnteringShortcut(p.room.GetWorldCoordinate(intVector2));
            }
        }

        public void TryWarpDrone(Player player, AbstractDMPSDrone abstractDMPSDrone)
        {
            if (player.room == null)
                return;
            abstractDMPSDrone.ChangeRooms(player.coord);
        }

        public void ThreatUpdate(Player player)
        {
            List<AbstractCreature> checkList = new List<AbstractCreature>();
            List<KeyValuePair<AbstractCreature, float>> threatList = new List<KeyValuePair<AbstractCreature, float>>();

            checkList.AddRange(player.room.abstractRoom.creatures);
            foreach(var creature in player.room.updateList.Where(u => u is Creature).Select(u => u as Creature))
            {
                if (checkList.Contains(creature.abstractCreature))
                    continue;
                checkList.Add(creature.abstractCreature);
            }


            for (int i = 0; i < checkList.Count; i++)
            {
                if (checkList[i].realizedCreature != null && checkList[i].realizedCreature != player)
                {

                    if (checkList[i].state == null || !checkList[i].state.alive) 
                        continue;
                    float danger = DMHelper.ThreatOfCreature(checkList[i].realizedCreature, player, true);
                    //float distance = Vector2.Distance(player.DangerPos, checkList[i].realizedCreature.DangerPos);

                    float threshold = 0.1f;
                    if (danger > threshold)
                    {
                        threatList.Add(new KeyValuePair<AbstractCreature, float> (checkList[i], danger));
                    }
                }
            }

            if (threatList.Count == 0)
            {
                foreach (var slate in droneSlates)
                    slate.Target = null;
                return;
            }

            threatList.Sort((x,y) => y.Value.CompareTo(x.Value));

            //todo 
            foreach(var slate in droneSlates)
            {
                slate.Target = threatList.First().Key;
            }
            //EmgTxCustom.Log($"DMPSDroneAI get target : {threatList.First().Key}");
        }

        internal class DroneSlate
        {
            DMPSDronePort port;
            AbstractDMPSDrone bindDrone;
            bool needAnim;

            public AbstractCreature Target
            {
                set
                {
                    if (bindDrone.abstractAI.RealAI != null)
                    {
                        (bindDrone.abstractAI.RealAI as DMPSDroneAI).target = value;
                    }
                }
            }

            public DroneSlate(DMPSDronePort port, Player player)
            {
                this.port = port;
                SpawnDrone(player);
            }

            public void UpdateSlate(Player player)
            {
                if (bindDrone.slatedForDeletion)
                {
                    if(port.noDroneCounter == 0)
                    {
                        bindDrone.addByPort = false;
                        SpawnDrone(player);
                    }
                    return;
                }

                if (bindDrone.abstractAI.RealAI == null)
                {
                    port.TryWarpDrone(player, bindDrone);
                }
                else
                {
                    (bindDrone.abstractAI.RealAI as DMPSDroneAI).ownerCoord = player.coord;

                    if(bindDrone.realizedCreature.room == player.room)
                        (bindDrone.abstractAI.RealAI as DMPSDroneAI).ownerInShortcut = false;
                }
            }

            void SpawnDrone(Player player)
            {
                bindDrone = new AbstractDMPSDrone(player.room.world, StaticWorld.GetCreatureTemplate(DMEnums.DMPS.CreatureTemplateType.DMPSDrone), null, player.coord, player.room.game.GetNewID())
                {
                    addByPort = true
                };
                player.room.abstractRoom.AddEntity(bindDrone);
                bindDrone.RealizeInRoom();
                needAnim = true;

                if (bindDrone.realizedCreature != null && bindDrone.realizedCreature.graphicsModule != null && player.graphicsModule != null)
                {
                    var drone = bindDrone.realizedCreature as DMPSDrone.DMPSDrone;
                    var anim = new ReleaseDroneAnim(drone, player);
                    drone.StartAnimation(anim);
                    player.room.AddObject(anim);
                    needAnim = false;
                }
            }

            public void OwnerEnteringShortcut(WorldCoordinate coordinate)
            {
                if (bindDrone.abstractAI.RealAI != null)
                {
                    (bindDrone.abstractAI.RealAI as DMPSDroneAI).ownerCoord = coordinate;
                    (bindDrone.abstractAI.RealAI as DMPSDroneAI).ownerInShortcut = true;
                }
            }
        }
    }


    internal class ReleaseDroneAnim : DMPSDrone.DMPSDrone.DroneAnimation
    {
        static int maxLife = 160;
        Player player;
        int counter;
        bool droneReleased;

        
        public ReleaseDroneAnim(DMPSDrone.DMPSDrone drone, Player player) : base(player.room, drone)
        {
            this.player = player;
            drone.disableJet = true;

            if (player.graphicsModule.internalContainerObjects == null)
                player.graphicsModule.internalContainerObjects = new List<GraphicsModule.ObjectHeldInInternalContainer>();

            player.graphicsModule.AddObjectToInternalContainer(drone.graphicsModule, 0);

            if (PlayerPatchs.TryGetModule<DMPSModule>(player, out var m))
            {
                var res = PSDronePortGraphics.PortArm.GetTipNJointPosNArmLength(player, m.port.armExtend, 1f);
                targetPos = res.tipPos;
            }

        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (player.room != room || player.room == null || drone.room == null || drone.room != room)
            {
                Destroy();
                return;
            }

            if (!PlayerPatchs.TryGetModule<DMPSModule>(player, out var m))
            {
                Destroy();
                return;
            }

            if (counter < maxLife)
            {
                counter++;
                if (counter == maxLife)
                {
                    Destroy();
                    return;
                }
            }

            if (counter < maxLife / 2)
            {
                float f = DMHelper.EaseInOutCubic(Mathf.InverseLerp(0f, maxLife / 2, counter));
                m.port.armExtend = f;
                //drone.firstChunk.lastPos = drone.firstChunk.pos;//更新顺序导致需要手动更改lastPos
                var res = PSDronePortGraphics.PortArm.GetTipNJointPosNArmLength(player, m.port.armExtend, 1f);

                var tile = room.GetTile(room.GetTilePosition(res.tipPos));
                if (tile.Solid || tile.AnyWater || tile.Terrain == Room.Tile.TerrainType.Slope)
                {
                    EmgReleaseDrone((targetPos - res.tipPos).normalized);
                    return;
                }

                targetPos = res.tipPos;
                targetDir = (res.tipPos - res.jointPos).normalized;
                wingStretch = f;

                
            }
            else if (counter == maxLife / 2)
                ReleaseDrone(m.port.armTipDir);
            else if(counter < maxLife)
                m.port.armExtend = DMHelper.EaseInOutCubic(1f - Mathf.InverseLerp(maxLife / 2, maxLife, counter));
        }

        void ReleaseDrone(Vector2 releaseDir)
        {
            if (droneReleased)
                return;

            if(drone.room != null)
            {
                drone.firstChunk.vel += releaseDir * 20f;
            }
            if(player.graphicsModule != null && drone.graphicsModule != null)
                player.graphicsModule.ReleaseSpecificInternallyContainedObjectSprites(drone.graphicsModule);

            drone.disableJet = false;
            drone.StartAnimation(null);
            droneReleased = true;

            wingStretch = 1f;
        }

        void EmgReleaseDrone(Vector2 releaseDir)
        {
            ReleaseDrone(releaseDir);
            if (PlayerPatchs.TryGetModule<DMPSModule>(player, out var m))
                m.port.armExtend = 0f;
        }

        public override void Destroy()
        {
            if (!droneReleased)
                ReleaseDrone(Vector2.up);

            if (!PlayerPatchs.TryGetModule<DMPSModule>(player, out var m))
                m.port.armExtend = 0f;
            wingStretch = 1f;
            slatedForDeletetion = true;
        }
    }
}
