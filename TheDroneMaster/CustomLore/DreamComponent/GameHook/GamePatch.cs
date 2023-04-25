using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DreamComponent.DreamHook;

namespace TheDroneMaster.GameHooks
{
    public class GamePatch
    {
        public static void Patch(RainWorld rainWorld)
        {
            CustomDreamHook.RegistryDream(new DroneMasterDream());
            //DreamSessionHook.RegisterDream(new DroneMasterDream());
            //OverWorldHooks.PatchOn();
            return;


            //ProcessManagerPatch.PatchOn(rainWorld);
            //RainWorldGamePatch.PatchOn();
            //SleepDeathScreenPatch.PatchOn();
        }
    }
}
