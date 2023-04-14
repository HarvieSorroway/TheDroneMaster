using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster
{
    public class GamePatch
    {
        public static void Patch()
        {

        }
    }

    public class ProcessManagerPatch
    {
        public static ConditionalWeakTable<ProcessManager, ProcessManagerModule> modules = new ConditionalWeakTable<ProcessManager, ProcessManagerModule>();
        public static void PatchOn()
        {
            On.ProcessManager.ctor += ProcessManager_ctor;
        }

        private static void ProcessManager_ctor(On.ProcessManager.orig_ctor orig, ProcessManager self, RainWorld rainWorld)
        {
            orig.Invoke(self, rainWorld);

        }

        public class ProcessManagerModule
    }
}
