using HUD;
using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS
{
    internal class DMPSSleepDeathScreen : Menu.Menu, IOwnAHUD
    {
        FContainer[] ladderContainers;
        MenuContainer[] hudContainers;

        SimpleButton continueButton;

        #region Map
        public int CurrentFood => 0;

        public Player.InputPackage MapInput => RWInput.PlayerInput(0);

        public bool RevealMap => false;

        public Vector2 MapOwnerInRoomPosition => Vector2.zero;

        public bool MapDiscoveryActive => false;

        public int MapOwnerRoom => -1;
        #endregion

        float ContinueAndExitButtonsXPos => manager.rainWorld.options.ScreenSize.x + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f;
        float LeftHandButtonsPosXAdd => Custom.LerpMap(this.manager.rainWorld.options.ScreenSize.x, 1024f, 1280f, 222f, 70f);

        public DMPSSleepDeathScreen(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            pages.Add(new Page(this, null, "main", 0));
            selectedObject = null;
        }

        public void FoodCountDownDone()
        {
            throw new NotImplementedException();
        }

        public HUD.HUD.OwnerType GetOwnerType()
        {
            throw new NotImplementedException();
        }

        public void PlayHUDSound(SoundID soundID)
        {
            throw new NotImplementedException();
        }

        public void AddContinueButton(bool black)
        {
            continueButton = new SimpleButton(this, pages[0], Translate("CONTINUE"), "CONTINUE", new Vector2(ContinueAndExitButtonsXPos - 180f - manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f)), new Vector2(110f, 30f));
            pages[0].subObjects.Add(continueButton);
            continueButton.black = (black ? 1f : 0f);
            pages[0].lastSelectedObject = continueButton;
        }

        public virtual void InitContainers()
        {
            ladderContainers = new FContainer[3];
            for (int i = 0; i < ladderContainers.Length; i++)
            {
                ladderContainers[i] = new FContainer();
            }
            pages[0].Container.AddChild(ladderContainers[0]);
            pages[0].Container.AddChild(ladderContainers[1]);
            hudContainers = new MenuContainer[2];
            for (int j = 0; j < 2; j++)
            {
                hudContainers[j] = new MenuContainer(this, pages[0], new Vector2(0f, 0f));
                pages[0].subObjects.Add(hudContainers[j]);
            }
            pages[0].Container.AddChild(ladderContainers[2]);
        }

        public override void Singal(MenuObject sender, string message)
        {
            if (message != null)
            {
                if (message == "CONTINUE")
                {
                    manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                    StartGame();
                    PlaySound(SoundID.MENU_Continue_From_Sleep_Death_Screen);
                    return;
                }
                if (!(message == "EXIT"))
                {
                    return;
                }
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                PlaySound(SoundID.MENU_Switch_Page_Out);
            }
        }

        public void StartGame()
        {
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }
    }
}
