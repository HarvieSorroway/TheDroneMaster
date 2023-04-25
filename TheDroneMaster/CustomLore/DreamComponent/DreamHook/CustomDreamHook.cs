using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.GameHooks;
using static TheDroneMaster.PlayerPatchs;

namespace TheDroneMaster.DreamComponent.DreamHook
{
    /// <summary>
    /// 自定义梦境，目前仅支持RainWorldGame类型的梦境（类似炸猫）
    /// Dream的生命周期：从 DreamScreen.Update 开始，到梦境结束后切换为 SleepAndDeathScreen ProcessManager.PostSwitchProcess 结束
    /// </summary>
    public class CustomDream
    {
        public bool dreamStarted = false;
        public bool dreamFinished = false;
        public bool currentDreamActivate = false;
        public DreamsState.DreamID activateDreamID;
        public SlugcatStats.Name focusSlugcat;

        /// <summary>
        /// 当前梦境是演出型梦境，还是cg梦境
        /// </summary>
        public virtual bool IsPerformDream => true;

        public CustomDream(SlugcatStats.Name focusSlugcat)
        {
            this.focusSlugcat = focusSlugcat;
        }

        public virtual void ActivateThisDream(DreamsState.DreamID dreamID)
        {
            activateDreamID = dreamID;
            currentDreamActivate = true;
            CustomDreamHook.currentActivateDream = this;

            Plugin.Log(ToString() + " dream activate, id : " + dreamID.ToString());
        }

        /// <summary>
        /// 梦境播放完成，在preSwitchProcess中清理状态
        /// </summary>
        public virtual void CleanUpThisDream()
        {
            dreamStarted  = false;
            dreamFinished = false;
            currentDreamActivate = false;
            CustomDreamHook.currentActivateDream = null;

            Plugin.Log(ToString() + "clean up dream");
        }

        /// <summary>
        /// 结束梦境调用的方法，仅当作为演出型梦境时被才需要被调用
        /// </summary>
        /// <param name="game"></param>
        public virtual void EndDream(RainWorldGame game)
        {
            Plugin.Log(ToString() + " try to end dream. already ended : " + dreamFinished.ToString());
            if (dreamFinished) return;

            dreamFinished = true;

            game.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            List<AbstractCreature> collection = new List<AbstractCreature>(game.session.Players);
            game.session = new StoryGameSession(PlayerModule.DroneMasterName, game);
            game.session.Players = new List<AbstractCreature>(collection);


            if (game.manager.musicPlayer != null)
            {
                game.manager.musicPlayer.FadeOutAllSongs(20f);
            }

            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SleepScreen, 5f);
        }

        /// <summary>
        /// 根据 DreamID 决定需要展示的 SceneID,仅当作为cg梦境的时候才需要重写该方法
        /// </summary>
        /// <param name="dreamID"></param>
        /// <returns></returns>
        public virtual MenuScene.SceneID SceneFromDream(DreamsState.DreamID dreamID)
        {
            return MenuScene.SceneID.Empty;
        }


        /// <summary>
        /// 决定该轮回结束后启用的梦境ID
        /// </summary>
        /// <param name="upcomingDream"></param>
        public virtual void DecideDreamID(
            SaveState saveState, 
            string currentRegion,
            string denPosition,
            ref int cyclesSinceLastDream,
            ref int cyclesSinceLastFamilyDream,
            ref int cyclesSinceLastGuideDream,
            ref int inGWOrSHCounter,
            ref DreamsState.DreamID upcomingDream,
            ref DreamsState.DreamID eventDream,
            ref bool everSleptInSB,
            ref bool everSleptInSB_S01,
            ref bool guideHasShownHimselfToPlayer,
            ref int guideThread,
            ref bool guideHasShownMoonThisRound,
            ref int familyThread)
        {
            if (dreamFinished) return;
        }

        public virtual CustomDreamHook.BuildDreamWorldParams GetBuildDreamWorldParams()
        {
            throw new NotImplementedException("Nooooo! you must implement this!");
        }

    }
    public class CustomDreamHook
    {
        public static List<CustomDream> dreams = new List<CustomDream>();
        public static CustomDream currentActivateDream;
        public static DataBridge dataBridge = new DataBridge();

        static bool registed = false;

        public static void RegistryDream(CustomDream customDream)
        {
            PatchOn();
            dreams.Add(customDream);
        }

        public static void PatchOn()
        {
            if (registed) return;
            On.DreamsState.StaticEndOfCycleProgress += DreamsState_StaticEndOfCycleProgress;
            On.Menu.DreamScreen.Update += DreamScreen_Update;
            On.Menu.DreamScreen.SceneFromDream += DreamScreen_SceneFromDream;

            On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;


            On.RainWorldGame.GameOver += RainWorldGame_GameOver;
            On.RainWorldGame.Win += RainWorldGame_Win;

            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            On.StoryGameSession.TimeTick += StoryGameSession_TimeTick;

            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState;
            On.PlayerProgression.SaveDeathPersistentDataOfCurrentState += PlayerProgression_SaveDeathPersistentDataOfCurrentState;

            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

            registed = true;
        }

        private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig.Invoke(self, ID);
            if (currentActivateDream != null && currentActivateDream.dreamStarted && currentActivateDream.dreamFinished)
            {
                currentActivateDream.CleanUpThisDream();
            }
        }

        private static void PlayerProgression_SaveDeathPersistentDataOfCurrentState(On.PlayerProgression.orig_SaveDeathPersistentDataOfCurrentState orig, PlayerProgression self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            Plugin.Log(String.Format("Inside PlayerProgression_SaveDeathPersistentDataOfCurrentState, {0}", currentActivateDream));
            if (currentActivateDream != null && currentActivateDream.IsPerformDream)
            {
                Plugin.Log(currentActivateDream.dreamStarted.ToString());
                if (currentActivateDream.dreamStarted)
                {
                    Plugin.Log(string.Format("PlayerProgression_SaveDeathPersistentDataOfCurrentState, block save : die {0}, quit {1}", saveAsIfPlayerDied, saveAsIfPlayerQuit));
                    return;
                }

            }
            orig.Invoke(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
        }

        private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, Menu.SleepAndDeathScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            if(dataBridge.sleepDeathScreenDataPackage != null)
            {
                package = dataBridge.sleepDeathScreenDataPackage;
                dataBridge.sleepDeathScreenDataPackage = null;
            }

            orig.Invoke(self, package);
        }

        private static SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            Plugin.Log(String.Format("Inside PlayerProgression_GetOrInitiateSaveState, {0}", currentActivateDream));
            if (currentActivateDream != null)
            {
                saveAsDeathOrQuit = !currentActivateDream.dreamStarted;
                Plugin.Log("Dream started , progression getOrInititateSaveState saveAsDeathOrQuit " + saveAsDeathOrQuit.ToString());
            }
            return orig.Invoke(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
        }

        private static void StoryGameSession_TimeTick(On.StoryGameSession.orig_TimeTick orig, StoryGameSession self, float dt)
        {
            if (currentActivateDream != null && currentActivateDream.dreamFinished)
                return;
            orig.Invoke(self, dt);
        }

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if(currentActivateDream != null && currentActivateDream.dreamStarted && currentActivateDream.IsPerformDream)
            {
                currentActivateDream.EndDream(self);
                return;
            }
            orig.Invoke(self, malnourished);
        }

        private static void RainWorldGame_GameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
        {
            if(currentActivateDream != null && currentActivateDream.dreamStarted && currentActivateDream.IsPerformDream)
            {
                currentActivateDream.EndDream(self);
                return;
            }
            orig.Invoke(self, dependentOnGrasp);
        }

        #region WorldLoader & OverWorld
        private static void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            if (currentActivateDream != null && currentActivateDream.IsPerformDream)
            {
                var param = currentActivateDream.GetBuildDreamWorldParams();

                string room = param.firstRoom;
                string region = room.Split('_').First().ToUpper();

                self.game.startingRoom = room;
                self.LoadWorld(region, self.PlayerCharacterNumber, param.singleRoomWorld);
                self.FIRSTROOM = room;

                Plugin.Log("OverWorld load room");
                return;
            }
            orig.Invoke(self);
        }
        private static void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, WorldLoader self)
        {
            if (self.game != null && self.game.session is StoryGameSession && currentActivateDream != null && currentActivateDream.IsPerformDream)
            {
                self.world.spawners = self.spawners.ToArray();
                List<World.Lineage> list = new List<World.Lineage>();
                for (int i = 0; i < self.spawners.Count; i++)
                {
                    if (self.spawners[i] is World.Lineage)
                    {
                        list.Add(self.spawners[i] as World.Lineage);
                    }
                }
                self.world.lineages = list.ToArray();
                if (self.loadContext == WorldLoader.LoadingContext.FASTTRAVEL || self.loadContext == WorldLoader.LoadingContext.MAPMERGE)
                {
                    self.world.LoadWorldForFastTravel(self.playerCharacter, self.abstractRooms, self.swarmRoomsList.ToArray(), self.sheltersList.ToArray(), self.gatesList.ToArray());
                }
                else
                {
                    self.world.LoadWorld(self.playerCharacter, self.abstractRooms, self.swarmRoomsList.ToArray(), self.sheltersList.ToArray(), self.gatesList.ToArray());
                    self.creatureStats[0] = (float)self.world.NumberOfRooms;
                    self.creatureStats[1] = (float)self.world.spawners.Length;
                }
                self.fliesMigrationBlockages = new int[self.tempBatBlocks.Count, 2];
                for (int j = 0; j < self.tempBatBlocks.Count; j++)
                {
                    int num = (self.world.GetAbstractRoom(self.tempBatBlocks[j].fromRoom) == null) ? -1 : self.world.GetAbstractRoom(self.tempBatBlocks[j].fromRoom).index;
                    int num2 = (self.world.GetAbstractRoom(self.tempBatBlocks[j].destRoom) == null) ? -1 : self.world.GetAbstractRoom(self.tempBatBlocks[j].destRoom).index;
                    self.fliesMigrationBlockages[j, 0] = num;
                    self.fliesMigrationBlockages[j, 1] = num2;
                }

                return;
            }
            orig.Invoke(self);
        }
        #endregion

        #region DreamScreen
        private static void DreamScreen_Update(On.Menu.DreamScreen.orig_Update orig, Menu.DreamScreen self)
        {
            orig.Invoke(self);
            if (self.manager.fadeToBlack > 0.9f && currentActivateDream != null && currentActivateDream.IsPerformDream)
            {
                self.manager.fadeToBlack = 1f;
                self.scene.RemoveSprites();

                currentActivateDream.dreamStarted = true;
                dataBridge.sleepDeathScreenDataPackage = self.fromGameDataPackage;
                self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
            }
        }

        private static MenuScene.SceneID DreamScreen_SceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, DreamScreen self, DreamsState.DreamID dreamID)
        {
            MenuScene.SceneID sceneID = orig.Invoke(self, dreamID);
            if(sceneID == MenuScene.SceneID.Empty && currentActivateDream != null && !currentActivateDream.IsPerformDream)
            {
                sceneID = currentActivateDream.SceneFromDream(dreamID);
                currentActivateDream.dreamStarted = true;
                currentActivateDream.dreamFinished = true;
            }
            return sceneID;
        }

        private static void DreamsState_StaticEndOfCycleProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
        {
            orig.Invoke(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
            if(dreams.Count > 0 )
            {
                foreach(var dream in dreams)
                {
                    if (dream.focusSlugcat != saveState.saveStateNumber)
                    {
                        Plugin.Log(string.Format("{0} not focus on this slugcat : {1}, skip decide dream", dream, saveState.saveStateNumber));
                        continue;
                    }

                    dream.DecideDreamID(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
                    if(upcomingDream != null)
                    {
                        dream.ActivateThisDream(upcomingDream);
                        break;
                    }
                }
            }
        }
        #endregion

        public class BuildDreamWorldParams
        {
            public string firstRoom;
            public bool singleRoomWorld;

            public SlugcatStats.Name playAs;
        }

        public class DataBridge
        {
            public KarmaLadderScreen.SleepDeathScreenDataPackage sleepDeathScreenDataPackage;
        }
    }

    public class DroneMasterDream : CustomDream
    {
        public static readonly DreamsState.DreamID DroneMasterDream_0 = new DreamsState.DreamID("DroneMasterDream_0", true);
        public static readonly DreamsState.DreamID DroneMasterDream_1 = new DreamsState.DreamID("DroneMasterDream_1", true);

        public DroneMasterDream() : base(new SlugcatStats.Name(Plugin.ID))
        {
        }

        public override void DecideDreamID(
            SaveState saveState,
            string currentRegion,
            string denPosition,
            ref int cyclesSinceLastDream,
            ref int cyclesSinceLastFamilyDream,
            ref int cyclesSinceLastGuideDream,
            ref int inGWOrSHCounter,
            ref DreamsState.DreamID upcomingDream,
            ref DreamsState.DreamID eventDream,
            ref bool everSleptInSB,
            ref bool everSleptInSB_S01,
            ref bool guideHasShownHimselfToPlayer,
            ref int guideThread,
            ref bool guideHasShownMoonThisRound,
            ref int familyThread)
        {
            if (dreamFinished) return;

            upcomingDream = null;
            cyclesSinceLastFamilyDream = 0;//屏蔽FamilyDream计数，防止被原本的方法干扰

            switch (familyThread)
            {
                case 0:
                    if(saveState.cycleNumber > 2 && cyclesSinceLastDream > 3)
                        upcomingDream = DroneMasterDream_0;
                    break;
                case 1:
                    if (cyclesSinceLastDream > 3)
                        upcomingDream = DroneMasterDream_1;
                    break;
            }
            if (upcomingDream != null)
            {
                familyThread++;
                cyclesSinceLastDream = 0;
            }
        }

        public override CustomDreamHook.BuildDreamWorldParams GetBuildDreamWorldParams()
        {
            if(activateDreamID == DroneMasterDream_0 ||
               activateDreamID == DroneMasterDream_1)
            {
                return new CustomDreamHook.BuildDreamWorldParams()
                {
                    firstRoom = "DMD_AI",
                    singleRoomWorld = false,

                    playAs = PlayerModule.DroneMasterName
                };
            }
            else
            {
                return null;
            }
        }
    }
}
