using MonoMod.RuntimeDetour;
using RWCustom;
using SlugBase;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using TheDroneMaster.CustomLore.SpecificScripts;
using UnityEngine;

namespace TheDroneMaster
{
    public class PlayerPatchs
    {
        public static ConditionalWeakTable<Player, PlayerModule> modules = new ConditionalWeakTable<Player, PlayerModule>();

        public delegate bool orig_Player_CanPutSpearToBack(Player self);
        static BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
        static BindingFlags methodFlags = BindingFlags.Static | BindingFlags.Public;

        public static void Patch()
        {
            On.Creature.SpitOutOfShortCut += Creature_SpitOutOfShortCut;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.checkInput += Player_checkInput;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.Player.Jump += Player_Jump;
            On.Player.SpearStick += Player_SpearStick;

            On.Player.ReleaseGrasp += Player_ReleaseGrasp;

            On.Player.Die += Player_Die;
            On.Player.Destroy += Player_Destroy;

            Hook player_get_CanPutSpearToBack_Hook = new Hook(typeof(Player).GetProperty("CanPutSpearToBack", propFlags).GetGetMethod(), typeof(PlayerPatchs).GetMethod("Player_get_CanPutSpearOnBack", methodFlags));
        }

        private static bool Player_SpearStick(On.Player.orig_SpearStick orig, Player self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
        {
            bool result = orig.Invoke(self, source, dmg, chunk, appPos, direction);

            if(modules.TryGetValue(self,out var module) && module.ownDrones)
            {
                if (module.playerDeathPreventer.DeathPreventCounter > 0 && module.playerDeathPreventer.AcceptableDamageCount >= 0) result = false; 
            }
            return result;
        }

        private static void Player_ReleaseGrasp(On.Player.orig_ReleaseGrasp orig, Player self, int grasp)
        {
            if (modules.TryGetValue(self, out var module) && module.ownDrones)
            {
                Creature creature = self.grasps[grasp].grabbed as Creature;
                if (creature != null)
                {
                    CreatureScanner scanner = new CreatureScanner(creature, self.DangerPos + new Vector2(0, 60), self.room, module.laserColor,module);
                    self.room.AddObject(scanner);
                }
            }
            orig.Invoke(self, grasp);
        }

        public static bool Player_get_CanPutSpearOnBack(orig_Player_CanPutSpearToBack orig,Player self)
        {
            bool result = orig.Invoke(self);
            if(modules.TryGetValue(self,out var module) && module.ownDrones)
            {
                result = result && Plugin.instance.config.canBackSpear.Value;
            }
            return result;
        }

        private static void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig.Invoke(self);
            bool getModule = modules.TryGetValue(self, out var module) && module.ownDrones;
            if (getModule && Plugin.instance.config.UsingPlayerInput.Value)
            {
                if (DroneHUD.instance.reval)
                {
                    Player.InputPackage current = new Player.InputPackage();
                    current.x = self.input[0].x;
                    current.y = self.input[0].y;
                    current.thrw = self.input[0].thrw;
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
            bool getModule = modules.TryGetValue(self, out var module) && module.ownDrones;
            if(getModule && module.lockMovementInput)
            {
                self.input[0] = new Player.InputPackage();
            }
            orig.Invoke(self, eu);
        }

        private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            if (modules.TryGetValue(self,out var module) && module.ownDrones)
            {
                if (module.port.availableDroneCount != 0) module.port.ClearOutAllDrones();
                module.playerDeathPreventer.AcceptableDamageCount = -1;
                if (!self.room.game.cameras[0].hud.textPrompt.gameOverMode)
                {
                    self.room.game.GameOver(null);
                }
            }
            orig.Invoke(self);
        }

        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            if(modules.TryGetValue(self, out var module) && module.ownDrones && !self.dead)
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
            if(modules.TryGetValue(self,out var module) && module.ownDrones)
            {
                self.jumpBoost *= 1.2f;
            }
        }

        private static void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig.Invoke(self,pos,newRoom,spitOutAllSticks);

            Player player = self as Player;
            if(player != null && modules.TryGetValue(player,out var module) && module.ownDrones && newRoom != null)
            {
                module.port.lastSpitOutShortcut = pos;
                module.port.lastSpitOutRoom = new WeakReference<Room>(newRoom);
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            bool getModule = modules.TryGetValue(self, out var module) && module.ownDrones;

            orig.Invoke(self, eu);
            
            if (getModule)
            {
                module.Update();

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

            if (Input.GetKeyDown(KeyCode.S))
            {
                //self.room.game.Win(false);
                if (Cool3DObject.instance != null) return;
                self.room.AddObject(new Cool3DObject(self.room, self.DangerPos));
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            modules.Add(self, new PlayerModule(self));
            //(self.abstractCreature.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = 9;
        }


        public class PlayerModule
        {
            public readonly WeakReference<Player> playerRef;
            public readonly SlugcatStats.Name name;
            public readonly SlugBaseCharacter character;

            public static SlugcatStats.Name DroneMasterName { get; private set; }

            public readonly int wirelessChargeNeeds = 400;

            public readonly bool ownDrones;
            public readonly bool usingDefaultCol = false;

            public readonly Color eyeColor;
            public readonly Color bodyColor;
            public readonly Color laserColor;

            #region WorldState
            public EnemyCreator enemyCreator;
            #endregion

            #region Graphics
            public bool graphicsInited = false;

            public MetalGills metalGills;
            public DronePortGraphics portGraphics;
            public int grillIndex = -1;
            public int newEyeIndex = -1;
            public int portIndex = -1;
            #endregion

            #region PlayeState
            public PlayerDeathPreventer playerDeathPreventer;
            public bool lockMovementInput = false;
            public bool fullCharge => ownDrones && playerRef.TryGetTarget(out var player) && player.playerState.foodInStomach == player.MaxFoodInStomach;
            #endregion

            public DronePort port;
            public PlayerModule(Player player)
            {
                ownDrones = Plugin.OwnLaserDrone.TryGet(player, out bool ownLaserDrone) && ownLaserDrone;
                playerRef = new WeakReference<Player>(player);
                
                //Plugin.Log(DeathPersistentSaveDataPatch.GetUnitOfHeader(EnemyCreator.header).ToString());

                if (ownDrones)
                {
                    ExtEnumBase extEnumBase;
                    playerDeathPreventer = new PlayerDeathPreventer(this);
                    bool canParse = ExtEnumBase.TryParse(typeof(SlugcatStats.Name), Plugin.ID, true, out extEnumBase);
                    if (canParse && name == null)
                    {
                        name = extEnumBase as SlugcatStats.Name;
                        if(DroneMasterName == null)
                        {
                            DroneMasterName = name;
                        }
                    }
                    //Plugin.Log(("Get PlayerName : " + name.value);
                    if (SlugBaseCharacter.TryGet(name, out character))
                    {
                        ColorSlot[] array;
                        bool flag4 = character.Features.TryGet<ColorSlot[]>(PlayerFeatures.CustomColors, out array);
                        if (flag4)
                        {
                            Plugin.Log(array.Length.ToString());

                            if (array.Length > 0)
                            {
                                bodyColor = array[0].GetColor(player.playerState.playerNumber);
                                Plugin.Log("eyecolor : " + ColorUtility.ToHtmlStringRGB(bodyColor));

                            }
                            if (array.Length > 1)
                            {
                                eyeColor = array[1].GetColor(player.playerState.playerNumber);
                                Plugin.Log("bodyColor : " + ColorUtility.ToHtmlStringRGB(eyeColor));
                            }
                            if (array.Length > 2)
                            {
                                laserColor = array[2].GetColor(player.playerState.playerNumber);
                                Plugin.Log("laserColor : " + ColorUtility.ToHtmlStringRGB(laserColor));
                            }
                        }
                        if (PlayerGraphics.customColors != null && !player.IsJollyPlayer)
                        {
                            if (PlayerGraphics.customColors.Count > 0)
                            {
                                bodyColor = PlayerGraphics.CustomColorSafety(0);
                                Plugin.Log("Custom-eyecolor : " + ColorUtility.ToHtmlStringRGB(bodyColor));
                            }
                            if (PlayerGraphics.customColors.Count > 1)
                            {
                                eyeColor = PlayerGraphics.CustomColorSafety(1);
                                Plugin.Log("Custom-bodyColor : " + ColorUtility.ToHtmlStringRGB(eyeColor));
                            }
                            if (PlayerGraphics.customColors.Count > 2)
                            {
                                laserColor = PlayerGraphics.CustomColorSafety(2);
                                Plugin.Log("Custom-laserColor : " + ColorUtility.ToHtmlStringRGB(laserColor));
                            }
                        }
                        if (PlayerGraphics.jollyColors != null && player.IsJollyPlayer)
                        {
                            bodyColor = PlayerGraphics.JollyColor(player.playerState.playerNumber, 0);
                            Plugin.Log("Jolly-eyecolor : " + ColorUtility.ToHtmlStringRGB(bodyColor));

                            eyeColor = PlayerGraphics.JollyColor(player.playerState.playerNumber, 1);
                            Plugin.Log("Jolly-bodyColor : " + ColorUtility.ToHtmlStringRGB(eyeColor));

                            laserColor = PlayerGraphics.JollyColor(player.playerState.playerNumber, 2);
                            Plugin.Log("Jolly-laserColor : " + ColorUtility.ToHtmlStringRGB(laserColor));
                        }

                        port = new DronePort(player);
                        if(Plugin.instance.config.moreEnemies.Value) enemyCreator = new EnemyCreator(this);
                        if(!Plugin.instance.config.canBackSpear.Value) player.spearOnBack = null;
                    }
                }
            }

            public void Update() // call in Player.Update
            {
                if(port != null) port.Update();
                if(playerDeathPreventer != null) playerDeathPreventer.Update();
                if(enemyCreator != null)enemyCreator.Update();
            }
        }
    }
}
