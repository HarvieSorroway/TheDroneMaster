using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.GameHooks
{
    public class ProcessManagerPatch
    {
        public static ConditionalWeakTable<ProcessManager, ProcessManagerModule> modules = new ConditionalWeakTable<ProcessManager, ProcessManagerModule>();
        public static void PatchOn(RainWorld rainWorld)
        {
            On.ProcessManager.ctor += ProcessManager_ctor;

            TryAddModule(rainWorld.processManager);
        }

        private static void ProcessManager_ctor(On.ProcessManager.orig_ctor orig, ProcessManager self, RainWorld rainWorld)
        {
            orig.Invoke(self, rainWorld);
            TryAddModule(self);
        }

        public static void TryAddModule(ProcessManager self)
        {
            if (!modules.TryGetValue(self, out var _))
            {
                modules.Add(self, new ProcessManagerModule(self));
            }
        }
    }

    public class ProcessManagerModule
    {
        public WeakReference<ProcessManager> managerRef;

        public int droneMasterDreamNumber = -1;

        public ProcessManagerModule(ProcessManager self)
        {
            managerRef = new WeakReference<ProcessManager>(self);
            Plugin.Log("Init new ProcessMangerModule");
        }
    }
}
