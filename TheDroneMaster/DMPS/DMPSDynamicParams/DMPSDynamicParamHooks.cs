using CustomSaveTx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSave;

namespace TheDroneMaster.DMPS.DMPSDynamicParams
{
    internal static class DMPSDynamicParamHooks
    {
        public static void HooksOn()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.RainWorldGame.Update += RainWorldGame_Update;
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig.Invoke(self);
            DMPSDynamicParams.DynamicParamInstance.Update();
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            DMPSDynamicParams.New();
        }
    }
}
