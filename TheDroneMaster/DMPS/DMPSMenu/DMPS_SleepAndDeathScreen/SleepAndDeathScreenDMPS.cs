using Menu;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.MenuHooks.KarmaLadderScreenHooks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSMenu.DMPS_SleepAndDeathScreen
{
    internal class SleepAndDeathScreenDMPS : Menu.Menu
    {
        SaveState saveState;

        MenuScene scene;
        SimpleButton continueButton, exitButton, skillTreeeButton;

        public float ContinueAndExitButtonsXPos => manager.rainWorld.options.ScreenSize.x + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f;
        public float LeftHandButtonsPosXAdd => Custom.LerpMap(this.manager.rainWorld.options.ScreenSize.x, 1024f, 1280f, 222f, 70f);

        public SleepAndDeathScreenDMPS(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            this.pages.Add(new Page(this, null, "main", 0));

            this.scene = new InteractiveMenuScene(this, this.pages[0], MenuScene.SceneID.SleepScreen);
            this.pages[0].subObjects.Add(this.scene);

            AddButton(false);
        }

        public virtual void GetDataFromGame(KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            Custom.Log(new string[]
            {
                string.Format("{0} screen get data from game! karma: {1} reinf: {2} sMal:{3} gMal:{4}", new object[]
                {
                    this.ID,
                    package.karma,
                    package.karmaReinforced,
                    package.startMalnourished,
                    package.goalMalnourished
                })
            });
            //this.karma = new IntVector2(Custom.IntClamp(package.karma.x + ((this.ID == ProcessManager.ProcessID.SleepScreen && package.karma.y < 100) ? -1 : 0), 0, package.karma.y), package.karma.y);
            //this.karmaReinforced = package.karmaReinforced;
            this.saveState = package.saveState;
            //this.myGamePackage = package;
            //this.playKarmaDream = false;
            //this.goalMalnourished = package.goalMalnourished;
            //this.dreamsState = null;
        }

        public void AddButton(bool black)
        {
            continueButton = new SimpleButton(this, pages[0], Translate("CONTINUE"), "CONTINUE", new Vector2(ContinueAndExitButtonsXPos - 180f - manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f)), new Vector2(110f, 30f));
            pages[0].subObjects.Add(continueButton);
            continueButton.black = (black ? 1f : 0f);
            pages[0].lastSelectedObject = continueButton;

            exitButton = new SimpleButton(this, pages[0], base.Translate("EXIT"), "EXIT", new Vector2(ContinueAndExitButtonsXPos - 320f - manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f)), new Vector2(110f, 30f));
            pages[0].subObjects.Add(exitButton);

            skillTreeeButton = new SimpleButton(this,
                    pages[0], DMPSResourceString.Get("PauseMenu_OpenSkillMenu"),
                    "DMPS_SKILLS",
                    new Vector2(ContinueAndExitButtonsXPos - 460f - manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f)),
                    new Vector2(110f, 30f));
            pages[0].subObjects.Add(skillTreeeButton);
        }

        public void StartGame()
        {
            if (ModManager.MMF && MMF.cfgLoadingScreenTips.Value && !this.manager.rainWorld.ExpeditionMode && this.saveState != null && TipScreen.AnyTipsAvailable(this.saveState.saveStateNumber, this.saveState.deathPersistentSaveData.tipCounter) && this.saveState.deathPersistentSaveData.deaths + this.saveState.deathPersistentSaveData.survives >= 4 && (this.saveState.deathPersistentSaveData.deaths + this.saveState.deathPersistentSaveData.survives) % TipScreen.GetCharacterTipFrequency(this.saveState.saveStateNumber) == 0)
            {
                this.manager.RequestMainProcessSwitch(MMFEnums.ProcessID.Tips);
                return;
            }
            this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message != null)
            {
                if (message == "CONTINUE")
                {
                    this.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                    this.StartGame();
                    base.PlaySound(SoundID.MENU_Continue_From_Sleep_Death_Screen);
                    return;
                }
                if (!(message == "EXIT"))
                {
                    return;
                }
                this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                base.PlaySound(SoundID.MENU_Switch_Page_Out);
            }
        }
    }
}
