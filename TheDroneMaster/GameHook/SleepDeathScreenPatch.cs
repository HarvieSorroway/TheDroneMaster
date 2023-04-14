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
            Plugin.Log("SleepScreen get data from game" + package.characterStats.name.ToString());
            orig.Invoke(self, package);
        }

        private static void SleepAndDeathScreen_Update(On.Menu.SleepAndDeathScreen.orig_Update orig, Menu.SleepAndDeathScreen self)
        {
            Plugin.Log("Sleep Screen update");
            orig.Invoke(self);

            if (ProcessManagerPatch.modules.TryGetValue(self.manager, out var managerModule))
            {
                if (managerModule.droneMasterDreamNumber != -1)
                {
                    if (self.manager.fadeToBlack > 0.1f)
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
