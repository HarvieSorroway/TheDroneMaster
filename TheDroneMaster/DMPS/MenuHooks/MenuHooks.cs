using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.SkillTree;
using TheDroneMaster.DMPS.SkillTree.SkillTreeRender;
using UnityEngine.Networking;

namespace TheDroneMaster.DMPS.MenuHooks
{
    internal static class MenuHooks
    {
        public static void HooksOn()
        {
            On.HUD.FoodMeter.TotalWidth += FoodMeter_TotalWidth;

            //test
            On.Menu.SlugcatSelectMenu.ctor += SlugcatSelectMenu_ctor;
            On.Menu.SlugcatSelectMenu.Update += SlugcatSelectMenu_Update;
            On.Menu.Menu.GrafUpdate += Menu_GrafUpdate;

        }

        private static void Menu_GrafUpdate(On.Menu.Menu.orig_GrafUpdate orig, Menu.Menu self, float timeStacker)
        {
            orig.Invoke(self, timeStacker);
            if(self is Menu.SlugcatSelectMenu)
            {
                testRenderer?.Draw(timeStacker);
            }
        }

        private static void SlugcatSelectMenu_Update(On.Menu.SlugcatSelectMenu.orig_Update orig, Menu.SlugcatSelectMenu self)
        {
            orig.Invoke(self);
            testRenderer?.Update();
            testLogic?.Update();
        }

        static SkillTreeRenderer testRenderer;
        static SkillTreeLogicBase testLogic;
        private static void SlugcatSelectMenu_ctor(On.Menu.SlugcatSelectMenu.orig_ctor orig, Menu.SlugcatSelectMenu self, ProcessManager manager)
        {
            SkillTreeInfo.ReadTreeInfo();
            orig.Invoke(self, manager);

            testRenderer = new SkillTreeRenderer();
            testLogic = new SkillTreeLogicBase();

            self.container.AddChild(testRenderer.container);
            testLogic.InitRenderer(testRenderer);
        }

        private static float FoodMeter_TotalWidth(On.HUD.FoodMeter.orig_TotalWidth orig, HUD.FoodMeter self, float timeStacker)
        {
            if (self.circles.Count == 0)
                return 0f;
            else
                return orig.Invoke(self, timeStacker);
        }
    }
}
