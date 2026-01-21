using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu;

namespace TheDroneMaster.DMPS.DMPSSkillTree
{
    internal static class SkillTreeHooks
    {
        public static void HooksOn()
        {
            On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig.Invoke(self);
            if (SkillTreeMenu.SkillTreeMenu.Instance != null)
                SkillTreeMenu.SkillTreeMenu.Instance.ShutDownProcess();
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig.Invoke(self);
            if (SkillTreeMenu.SkillTreeMenu.Instance != null)
                SkillTreeMenu.SkillTreeMenu.Instance.Update();
        }

        private static void RainWorldGame_GrafUpdate(On.RainWorldGame.orig_GrafUpdate orig, RainWorldGame self, float timeStacker)
        {
            orig.Invoke(self, timeStacker);
            if (SkillTreeMenu.SkillTreeMenu.Instance != null)
                SkillTreeMenu.SkillTreeMenu.Instance.GrafUpdate(timeStacker);
        }
    }
}
