using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSMenu.DMPS_SleepAndDeathScreen
{
    internal static class SleepAndDeathScreenHooks
    {
        public static void HooksOn()
        {
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
        }

        private static void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if(self.pendingProcess == null)
            {
                if(ID == DMEnums.DMPS.ProcessManger.ProcessID.SleepDMPS || ID == DMEnums.DMPS.ProcessManger.ProcessID.DeathDMPS)
                {
                    self.currentMainLoop = new SleepAndDeathScreenDMPS(self, ID);
                }
            }
            orig.Invoke(self, ID);
        }
    }
}
