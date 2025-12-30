using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.MenuHooks.KarmaLadderScreenHooks
{
    internal static class KarmaLadderScreenHooks
    {
        public static void HooksOn()
        {
            //On.Menu.KarmaLadderScreen.AddSubObjects += KarmaLadderScreen_AddSubObjects;
        }

        private static void KarmaLadderScreen_AddSubObjects(On.Menu.KarmaLadderScreen.orig_AddSubObjects orig, Menu.KarmaLadderScreen self)
        {
            if (self.saveState != null && self.saveState.saveStateNumber == DMEnums.SlugStateName.DMPS)
            {
                return;
            }
            else
                orig.Invoke(self);
        }
    }
}
