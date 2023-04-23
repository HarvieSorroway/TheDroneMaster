using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.GameHooks;

namespace TheDroneMaster.GameHook
{
    public class SleepDeathScreenPatch
    {
        public static void PatchOn()
        {
            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
            On.Menu.SleepAndDeathScreen.Update += SleepAndDeathScreen_Update;
        }


        private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, Menu.SleepAndDeathScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            var game = self.manager.oldProcess as RainWorldGame;
            ProcessManagerPatch.modules.TryGetValue(self.manager, out var managerModule);
            if (game != null && RainWorldGamePatch.modules.TryGetValue(game, out var module))
            {
                if (module.IsDroneMasterDream && module.packageFromSleepScreen != null)
                {
                    package = module.packageFromSleepScreen;
                    package.saveState = managerModule.saveStateBuffer;
                    managerModule.tempProgressionBuffer.currentSaveState = package.saveState;

                    managerModule.saveStateBuffer = null;
                    managerModule.tempProgressionBuffer = null;
                }
                else
                {
                    if (managerModule.saveStateBuffer == null) managerModule.saveStateBuffer = package.saveState;
                }

            }

            orig.Invoke(self, package);
        }

        private static void SleepAndDeathScreen_Update(On.Menu.SleepAndDeathScreen.orig_Update orig, Menu.SleepAndDeathScreen self)
        {
            orig.Invoke(self);

            if (ProcessManagerPatch.modules.TryGetValue(self.manager, out var managerModule))
            {
                if (managerModule.droneMasterDreamNumber != -1)
                {
                    if (self.manager.fadeToBlack < 0.9f)
                    {
                        self.manager.fadeToBlack = 1f;
                        self.scene.RemoveSprites();

                        self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                    }
                }
            }
        }
    }
}
