using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree
{
    internal static class SkillTreeHooks
    {
        public static void HooksOn()
        {
            //On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
            //On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;

            On.Menu.PauseMenu.SpawnExitContinueButtons += PauseMenu_SpawnExitContinueButtons;
            On.Menu.PauseMenu.Singal += PauseMenu_Singal;
            On.Menu.PauseMenu.Update += PauseMenu_Update;
        }

        private static void PauseMenu_Update(On.Menu.PauseMenu.orig_Update orig, PauseMenu self)
        {
            if(SkillTreeMenu.SkillTreeMenu.Instance != null)
                return;
            orig.Invoke(self);
        }

        private static void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
        {
            orig.Invoke(self, sender, message);
            if(message == "DMPS_SKILLS")
            {
                SkillTreeMenu.SkillTreeMenu.OpenSkillTree(self.game.rainWorld.processManager, self, true);
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

                if (self.continueButton != null)
                {
                    self.continueButton.RemoveSprites();
                    self.pages[0].RemoveSubObject(self.continueButton);
                }
                self.continueButton = null;
                if (self.exitButton != null)
                {
                    self.exitButton.RemoveSprites();
                    self.pages[0].RemoveSubObject(self.exitButton);
                }
                self.exitButton = null;
                sender.RemoveSprites();
                self.pages[0].RemoveSubObject(sender);
            }
        }

        private static void PauseMenu_SpawnExitContinueButtons(On.Menu.PauseMenu.orig_SpawnExitContinueButtons orig, Menu.PauseMenu self)
        {
            orig.Invoke(self);
            if (self.game.session is StoryGameSession storySession && Custom.rainWorld.inGameSlugCat == DMEnums.DMPS.SlugStateName.DMPS)
            {
                var skillTreeButton = new SimpleButton(self, self.pages[0], 
                    DMPSResourceString.Get("PauseMenu_OpenSkillMenu"), 
                    "DMPS_SKILLS", 
                    new Vector2(self.ContinueAndExitButtonsXPos - 460.2f - self.moveLeft - self.manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f)), 
                    new Vector2(110f, 30f));
                self.pages[0].subObjects.Add(skillTreeButton);

                skillTreeButton.nextSelectable[1] = skillTreeButton;
                skillTreeButton.nextSelectable[3] = skillTreeButton;
            }
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
