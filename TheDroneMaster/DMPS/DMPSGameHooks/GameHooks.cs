using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSMenu.DMPS_SleepAndDeathScreen;
using static Expedition.ExpeditionProgression;

namespace TheDroneMaster.DMPS.DMPSGameHooks
{
    /// <summary>
    /// 服务于SleepAndDeathScreenDMPS
    /// </summary>
    internal static class GameHooks
    {
        public static void HooksOn()
        {
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            On.RainWorldGame.CommunicateWithUpcomingProcess += RainWorldGame_CommunicateWithUpcomingProcess;
        }

        private static void RainWorldGame_CommunicateWithUpcomingProcess(On.RainWorldGame.orig_CommunicateWithUpcomingProcess orig, RainWorldGame self, MainLoopProcess nextProcess)
        {
            orig.Invoke(self, nextProcess);
            if(nextProcess is SleepAndDeathScreenDMPS sleepDeathScreen)
            {
                int currKarma = self.GetStorySession.saveState.deathPersistentSaveData.karma;
                int nextKarma = currKarma;

                if(sleepDeathScreen.ID == DMEnums.DMPS.ProcessManger.ProcessID.DeathDMPS && !self.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma)
                {
                    nextKarma = Custom.IntClamp(nextKarma - 1, 0, self.GetStorySession.saveState.deathPersistentSaveData.karmaCap);
                }

                if (self.cameras[0].hud != null)
                {
                    self.cameras[0].hud.map.mapData.UpdateData(self.world, 1 + self.GetStorySession.saveState.deathPersistentSaveData.foodReplenishBonus, nextKarma, self.GetStorySession.saveState.deathPersistentSaveData.karmaFlowerPosition, true);
                }

                SleepAndDeathScreenDMPS.DMPSSleepAndDeathScreenDataPackage package = new SleepAndDeathScreenDMPS.DMPSSleepAndDeathScreenDataPackage(nextKarma, self.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma, self.GetStorySession.saveState);
                sleepDeathScreen.GetDataFromGame(package);
            }
        }

        private static void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
        {
            orig.Invoke(self);

            ModifyUpcomingProcess(self);
        }

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
        {
            orig.Invoke(self, malnourished, fromWarpPoint);

            ModifyUpcomingProcess(self);
        }

        static void ModifyUpcomingProcess(RainWorldGame self)
        {
            //暂时屏蔽dream
            if (self.StoryCharacter == DMEnums.DMPS.SlugStateName.DMPS)
            {
                //Plugin.Log("Try modify upcoming sleep screen");
                if (self.manager._processSwitchQueue.Count > 0)
                {
                    var request = self.manager._processSwitchQueue.Dequeue();
                    if (request.ID == ProcessManager.ProcessID.SleepScreen ||
                       request.ID == ProcessManager.ProcessID.Dream)
                        request.ID = DMEnums.DMPS.ProcessManger.ProcessID.SleepDMPS;
                    else if (request.ID == ProcessManager.ProcessID.DeathScreen)
                        request.ID = DMEnums.DMPS.ProcessManger.ProcessID.DeathDMPS;
                    self.manager._processSwitchQueue.Enqueue(request);
                }
                else
                {
                    if (self.manager.upcomingProcess == ProcessManager.ProcessID.SleepScreen ||
                        self.manager.upcomingProcess == ProcessManager.ProcessID.Dream)
                        self.manager.upcomingProcess = DMEnums.DMPS.ProcessManger.ProcessID.SleepDMPS;
                    else if (self.manager.upcomingProcess == ProcessManager.ProcessID.DeathScreen)
                        self.manager.upcomingProcess = DMEnums.DMPS.ProcessManger.ProcessID.DeathDMPS;
                }
            }
        }
    }
}
