using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DreamComponent.DreamHook;
using TheDroneMaster.GameHook;

namespace TheDroneMaster.GameHooks
{
    public class GamePatch
    {
        public static void Patch(RainWorld rainWorld)
        {

            return;

            OverWorldHooks.PatchOn();
            ProcessManagerPatch.PatchOn(rainWorld);
            RainWorldGamePatch.PatchOn();
            SleepDeathScreenPatch.PatchOn();
        }
    }
}
