using CustomDreamTx;
using CustomSaveTx;
using DMPS.PlayerHooks;
using MonoMod.RuntimeDetour;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TheDroneMaster.CustomLore.SpecificScripts;
using TheDroneMaster.DreamComponent.DreamHook;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster
{
    public class PlayerPatchs
    {
        public static ConditionalWeakTable<Player, PlayerModule> modules = new ConditionalWeakTable<Player, PlayerModule>();

        public delegate bool orig_Player_CanPutSpearToBack(Player self);
        public static BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
        public static BindingFlags methodFlags = BindingFlags.Static | BindingFlags.Public;

        public static bool TryGetModule<T>(Player p, out T module) where T : PlayerModule
        {
            if(modules.TryGetValue(p, out var m) && m is T res)
            {
                module = res;
                return true;
            }
            module = null;
            return false;
        }

        public static void Patch()
        {
            On.Creature.SpitOutOfShortCut += Creature_SpitOutOfShortCut;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.checkInput += Player_checkInput;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.Jump += Player_Jump;
            On.Player.SpearStick += Player_SpearStick;

            On.Player.TossObject += Player_TossObject;

            On.Player.Die += Player_Die;
            On.Player.Destroy += Player_Destroy;

            Hook player_get_CanPutSpearToBack_Hook = new Hook(typeof(Player).GetProperty("CanPutSpearToBack", propFlags).GetGetMethod(), typeof(PlayerPatchs).GetMethod("Player_get_CanPutSpearOnBack", methodFlags));
        }

        private static void Player_TossObject(On.Player.orig_TossObject orig, Player self, int grasp, bool eu)
        {
            if (!Plugin.SkinOnly && modules.TryGetValue(self, out PlayerModule module) && module.isStoryGamePlayer && self.grasps[grasp] != null && self.room != null && module is DroneMasterModule DMM)
            {
                Creature creature = self.grasps[grasp].grabbed as Creature;
                if (creature != null && creature.dead && !DeathPersistentSaveDataRx.GetTreatmentOfType<ScannedCreatureSaveUnit>().IsThisTypeScanned(creature.abstractCreature.creatureTemplate.type))
                {
                    CreatureScanner scanner = new CreatureScanner(creature, self.DangerPos + new Vector2(0, 60), self.room, DMM.laserColor, DMM);
                    self.room.AddObject(scanner);
                }
            }
            orig.Invoke(self, grasp, eu);
        }

        private static bool Player_SpearStick(On.Player.orig_SpearStick orig, Player self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
        {
            bool result = orig.Invoke(self, source, dmg, chunk, appPos, direction);

            if(modules.TryGetValue(self,out var module) && !Plugin.SkinOnly)
            {
                if (module.playerDeathPreventer.DeathPreventCounter > 0 && module.playerDeathPreventer.AcceptableDamageCount >= 0) result = false; 
            }
            return result;
        }

        public static bool Player_get_CanPutSpearOnBack(orig_Player_CanPutSpearToBack orig,Player self)
        {
            bool result = orig.Invoke(self);
            if(modules.TryGetValue(self,out var module))
            {
                result = result && Plugin.instance.config.canBackSpear.Value;
            }
            return result;
        }

        private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig.Invoke(self);
            bool getModule = modules.TryGetValue(self, out var module) && module is DroneMasterModule;
            if (getModule && Plugin.instance.config.UsingPlayerInput.Value && !Plugin.SkinOnly)
            {
                if (PlayerDroneHUD.instance.reval)
                {
                    Player.InputPackage current = new Player.InputPackage();
                    current =self.input[0];
                    module.lockMovementInput = true;
                    DroneHUDInputManager.GetPlayerInput(current);
                }
                else
                {
                    module.lockMovementInput = false;
                }
            }
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            bool getModule = modules.TryGetValue(self, out var module);
            if(getModule && module.lockMovementInput && !Plugin.SkinOnly)
            {
                //确保玩家依旧可以关闭HUD
                var newInput = new Player.InputPackage();
                newInput.spec=self.input[0].spec;
                //锁定玩家输入防止乱跑
                self.input[0] = newInput;
            }
            orig.Invoke(self, eu);
        }

        private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            if (modules.TryGetValue(self, out var module) && !Plugin.SkinOnly)
            {
                if (module is DroneMasterModule DMM)
                {
                    if (DMM.port.availableDroneCount != 0) 
                        DMM.port.ClearOutAllDrones();
                    DMM.playerDeathPreventer.AcceptableDamageCount = -1;
                    if (!self.room.game.cameras[0].hud.textPrompt.gameOverMode)
                    {
                        self.room.game.GameOver(null);
                    }
                }
            }
            orig.Invoke(self);
        }

        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            if(modules.TryGetValue(self, out var module) && !self.dead && !Plugin.SkinOnly)
            {
                if (module.playerDeathPreventer.canTakeDownThisDamage(self,"player die"))
                {
                    self.dead = false;
                    self.aerobicLevel = 0f;
                    return;
                }
            }

            orig.Invoke(self);
        }

        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig.Invoke(self);
            if(modules.TryGetValue(self, out var module))
            {
                self.jumpBoost *= 1.2f;
            }
        }

        private static void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig.Invoke(self,pos,newRoom,spitOutAllSticks);

            Player player = self as Player;
            if(player != null && modules.TryGetValue(player, out var module) && newRoom != null)
            {
                if(module is DroneMasterModule DMM)
                {
                    DMM.port.lastSpitOutShortcut = pos;
                    DMM.port.lastSpitOutRoom = new WeakReference<Room>(newRoom);
                }
       
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            bool getModule = modules.TryGetValue(self, out var module);

            orig.Invoke(self, eu);


            if (getModule)
            {
                module.Update(self);

                //if some creature like lizard grab player for more than 60 frames, record as the player's death
                if (self.dangerGraspTime == 60 && self.AI == null)
                {
                    if (module.playerDeathPreventer.canTakeDownThisDamage(self, "player update"))
                    {
                        self.dead = false;
                        self.aerobicLevel = 0f;
                        if (self.room != null)
                        {
                            if (self.room.world.game.cameras[0].hud.textPrompt.gameOverMode) self.room.world.game.cameras[0].hud.textPrompt.gameOverMode = false;
                        }
                    }
                    else self.Die();
                }
            }

            //if (Input.GetKeyDown(KeyCode.N))
            //{
            //    self.room.game.Win(false);
            //    //self.room.game.rainWorld.progression.currentSaveState.deathPersistentSaveData.altEnding = true;
            //    //self.room.game.GoToRedsGameOver();

            //    //self.room.AddObject(new MeshTest(self));
            //    //if (Simple3DObject.instance != null) return;
            //    //    self.room.AddObject(new Simple3DObject(self.room, self));

            //}
            //if (Input.GetKeyDown(KeyCode.K))
            //{
            //    Plugin.Log(self.abstractCreature.pos.Tile.ToString());
            //}

            //if(Random.value < 0.3f)
            //{
            //    int randomX = (int)Random.Range(0, self.room.TileWidth);
            //    int randomY = (int)Random.Range(0, self.room.TileHeight);

            //    if(self.room.GetTile(randomX, randomY).Solid)
            //    {
            //        self.room.AddObject(new Lightning(self.room, self.DangerPos, self.room.MiddleOfTile(randomX, randomY)));
            //    }
            //}
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            if(self.slugcatStats.name == DMEnums.SlugStateName.DroneMaster )
            {
                modules.Add(self, new DroneMasterModule(self));
            }
            else if(self.slugcatStats.name == DMEnums.DMPS.SlugStateName.DMPS)
            {
                modules.Add(self, new DMPSModule(self));
            }
               
            //(self.abstractCreature.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = 9;
        }

    }

    

    public class DreamStateOverride
    {
        public int availableDroneCount;
        public int overrideHealth;
        public bool initDronePortGraphics = false;
        public Vector2 dronePortGraphicsPos = Vector2.zero;
        public Vector2 currentPortPos = Vector2.zero;
        public float dronePortGraphicsRotation = 0f;

        public float connectToDMProggress = 1f;

        public DreamStateOverride(int availableDroneCount, bool initDronePortGraphics, Vector2 dronePortGraphicsPos, float dronePortGraphicsRotation,int overrideHealth)
        {
            this.availableDroneCount = availableDroneCount;
            this.initDronePortGraphics = initDronePortGraphics;
            this.dronePortGraphicsPos = dronePortGraphicsPos;
            this.dronePortGraphicsRotation = dronePortGraphicsRotation;
            this.overrideHealth = overrideHealth;

            Plugin.Log("Set up override {0},{1},{2},{3}", availableDroneCount, initDronePortGraphics, dronePortGraphicsPos, dronePortGraphicsRotation);
        }
    }
}
