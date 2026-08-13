using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu;
using UnityEngine;

namespace TheDroneMaster.DMPS.MenuHooks.KarmaLadderScreenHooks
{
    internal static class KarmaLadderScreenHooks
    {
        public static void HooksOn()
        {
            On.Menu.SleepAndDeathScreen.AddSubObjects += SleepAndDeathScreen_AddSubObjects;
            On.Menu.SleepAndDeathScreen.Update += SleepAndDeathScreen_Update;
            On.Menu.SleepAndDeathScreen.Singal += SleepAndDeathScreen_Singal;
        }

        private static void SleepAndDeathScreen_Singal(On.Menu.SleepAndDeathScreen.orig_Singal orig, Menu.SleepAndDeathScreen self, Menu.MenuObject sender, string message)
        {
            orig.Invoke(self, sender, message);
            if(message == "DMPS_SKILLS")
            {
                self.PlaySound(SoundID.MENU_Passage_Button);
                SkillTreeMenu.OpenSkillTree(self.manager, null, false);
            }
        }

        private static void SleepAndDeathScreen_Update(On.Menu.SleepAndDeathScreen.orig_Update orig, Menu.SleepAndDeathScreen self)
        {
            if (SkillTreeMenu.Instance != null)
                return;
            orig.Invoke(self);
        }

        private static void SleepAndDeathScreen_AddSubObjects(On.Menu.SleepAndDeathScreen.orig_AddSubObjects orig, Menu.SleepAndDeathScreen self)
        {
            orig.Invoke(self);
            if(self.saveState.saveStateNumber == DMEnums.DMPS.SlugStateName.DMPS)
            {
                var sleepAndDeathScreenButton = new SleepAndDeathScreenButton(self, 
                    self.pages[0],DMPSResourceString.Get("PauseMenu_OpenSkillMenu"),
                    "DMPS_SKILLS",
                    new Vector2(self.ContinueAndExitButtonsXPos - 460f - self.manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f)), 
                    new Vector2(110f, 30f));
                self.pages[0].subObjects.Add(sleepAndDeathScreenButton);
            }
        }
    }
}
