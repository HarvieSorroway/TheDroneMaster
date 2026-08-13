using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPShud;
using TheDroneMaster.DMPS.SkillTree;
using TheDroneMaster.DMPS.SkillTree.SkillTreeRender;
using UnityEngine.Networking;

namespace TheDroneMaster.DMPS.MenuHooks
{
    internal static class MenuHooks
    {
        public static void HooksOn()
        {
            ContinueSlugPageHooks.HooksOn();
            KarmaLadderScreenHooks.KarmaLadderScreenHooks.HooksOn();
        }

    }
}
