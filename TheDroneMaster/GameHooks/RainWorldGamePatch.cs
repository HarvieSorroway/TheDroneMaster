using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.GameHooks
{
    public class RainWorldGamePatch
    {
        public static ConditionalWeakTable<RainWorldGame, RainWorldGameModule> modules = new ConditionalWeakTable<RainWorldGame, RainWorldGameModule>();
        public static void PatchOn()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            if(!modules.TryGetValue(self,out var _))
            {
                modules.Add(self, new RainWorldGameModule(self, manager));
            }
        }
    }

    public class RainWorldGameModule
    {
        public WeakReference<RainWorldGame> gameRef;

        public readonly bool isDroneMasterDream;
        public readonly int currentDroneMasterDreamNumber = -1;

        public RainWorldGameModule(RainWorldGame game,ProcessManager manager)
        {
            gameRef = new WeakReference<RainWorldGame>(game);

            if(ProcessManagerPatch.modules.TryGetValue(manager,out var managerModule))
            {
                if(managerModule.droneMasterDreamNumber != -1)
                {
                    isDroneMasterDream = true;
                    managerModule.droneMasterDreamNumber = -1;
                }

                currentDroneMasterDreamNumber = managerModule.droneMasterDreamNumber;
            }
        }
    }
}
