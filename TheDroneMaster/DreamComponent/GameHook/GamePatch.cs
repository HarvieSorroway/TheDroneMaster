using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.GameHook;

namespace TheDroneMaster.GameHooks
{
    public class GamePatch
    {
        public static void Patch(RainWorld rainWorld)
        {
            OverWorldHooks.PatchOn();
            ProcessManagerPatch.PatchOn(rainWorld);
            RainWorldGamePatch.PatchOn();
            SleepDeathScreenPatch.PatchOn();
        }
    }
}
