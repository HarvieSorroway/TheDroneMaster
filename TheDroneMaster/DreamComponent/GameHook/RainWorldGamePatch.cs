using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static TheDroneMaster.PlayerPatchs;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using Menu;

namespace TheDroneMaster.GameHooks
{
    public class RainWorldGamePatch
    {
        public static ConditionalWeakTable<RainWorldGame, RainWorldGameModule> modules = new ConditionalWeakTable<RainWorldGame, RainWorldGameModule>();
        public static void PatchOn()
        {
            //IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;

            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GameOver += RainWorldGame_GameOver;

            On.StoryGameSession.TimeTick += StoryGameSession_TimeTick;
        }

        private static void StoryGameSession_TimeTick(On.StoryGameSession.orig_TimeTick orig, StoryGameSession self, float dt)
        {
            if (modules.TryGetValue(self.game, out var module))
            {
                if (module.IsDroneMasterDream && module.DreamFinished)
                {
                    return;
                }
            }
            orig.Invoke(self, dt);
        }

        private static void RainWorldGame_RawUpdate(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
               i => i.MatchLdarg(0),
               i => i.Match(OpCodes.Ldfld),
               i => i.MatchLdfld<ProcessManager>("artificerDreamNumber"),
               i => i.Match(OpCodes.Ldc_I4_M1)
            ))
            {
                try
                {
                    var skipLabel = c.MarkLabel();
                    //Plugin.Log(c.Body.ToString());
                    c.Index++;

                    c.Emit(OpCodes.Ldarg_0);

                    //返回真时跳过,意味着Session.TimeTick会被跳过
                    c.EmitDelegate<Func<RainWorldGame, bool>>((self) =>
                    {
                        if (modules.TryGetValue(self, out var module))
                        {
                            if (ProcessManagerPatch.modules.TryGetValue(self.manager, out var managerModule))
                            {
                                if (module.IsDroneMasterDream && managerModule.droneMasterDreamNumber == -1)
                                {
                                    Plugin.Log("Skip label");
                                    return true;
                                }
                            }
                        }
                        return false;
                    });

                    c.Emit(OpCodes.Brtrue_S, skipLabel);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static void RainWorldGame_GameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
        {
            if (modules.TryGetValue(self, out var module) && module.IsDroneMasterDream)
            {
                module.EndDroneMasterDream(self);
                return;
            }
            orig.Invoke(self, dependentOnGrasp);
        }

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (modules.TryGetValue(self, out var module))
            {
                ProcessManagerModule managerModule = null;
                if (!module.IsDroneMasterDream)
                {
                    if (ProcessManagerPatch.modules.TryGetValue(self.manager, out managerModule))
                    {
                        managerModule.droneMasterDreamNumber = 1;
                        managerModule.tempProgressionBuffer = self.rainWorld.progression;
                    }
                }
                else return;

                Plugin.Log(String.Format("RainWorldGame win, {0}, {1}, {2}", module.IsDroneMasterDream, managerModule, managerModule.tempProgressionBuffer));
            }
            orig.Invoke(self, malnourished);
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig.Invoke(self);
            if (modules.TryGetValue(self, out var module))
            {
                module.Update(self);
            }
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            if (!modules.TryGetValue(self, out var _))
            {
                modules.Add(self, new RainWorldGameModule(self, manager));
            }

            //Plugin.Log(String.Format("SaveState current karma : {0}", self.GetStorySession.saveState.deathPersistentSaveData.karma));
        }
    }

    public class RainWorldGameModule
    {
        public WeakReference<RainWorldGame> gameRef;

        public readonly int currentDroneMasterDreamNumber = -1;

        public bool IsDroneMasterDream => currentDroneMasterDreamNumber != -1;
        public bool DreamFinished { get; private set; }

        public KarmaLadderScreen.SleepDeathScreenDataPackage packageFromSleepScreen;

        public RainWorldGameModule(RainWorldGame game, ProcessManager manager)
        {
            gameRef = new WeakReference<RainWorldGame>(game);

            if (ProcessManagerPatch.modules.TryGetValue(manager, out var managerModule))
            {
                currentDroneMasterDreamNumber = managerModule.droneMasterDreamNumber;

                if (manager.oldProcess is Menu.SleepAndDeathScreen && IsDroneMasterDream)
                {
                    var sleepScreen = manager.oldProcess as Menu.SleepAndDeathScreen;
                    packageFromSleepScreen = sleepScreen.myGamePackage;

                    game.rainWorld.progression = managerModule.tempProgressionBuffer;
                    game.startingRoom = "DMD_A01";
                }
                Plugin.Log(String.Format("Init new RainWorldGame module, droneMasterDreamNumber : {0}\nPrevious mainLoopProcess : {1}", currentDroneMasterDreamNumber, manager.oldProcess));
            }
        }

        public void Update(RainWorldGame self)
        {
            if (IsDroneMasterDream && self.processActive && !DreamFinished)
            {
                var session = self.GetStorySession;
                if (session == null || session.playerSessionRecords == null) return;

                if (session.playerSessionRecords[0].time > 1600)
                {
                    EndDroneMasterDream(self);
                }
                else
                {
                    //Plugin.Log(String.Format("DroneMaster in dream ticks : {0}", session.playerSessionRecords[0].time));
                }
            }
        }

        public void EndDroneMasterDream(RainWorldGame self)
        {
            if (!ProcessManagerPatch.modules.TryGetValue(self.manager, out var managerModule))
            {
                Plugin.Log("Manger module lost, stop end dronemaster dream");
                return;
            }
            if (DreamFinished) return;

            managerModule.droneMasterDreamNumber = -1;
            DreamFinished = true;

            self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            List<AbstractCreature> collection = new List<AbstractCreature>(self.session.Players);
            self.session = new StoryGameSession(PlayerModule.DroneMasterName, self) { saveState = managerModule.saveStateBuffer };
            self.session.Players = new List<AbstractCreature>(collection);



            if (self.manager.musicPlayer != null)
            {
                self.manager.musicPlayer.FadeOutAllSongs(20f);
            }

            self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SleepScreen, 10f);
        }
    }
}
