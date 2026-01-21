using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu
{
    internal class SkillTreeButtonBehaviour
    {
        public float lastCol, col;
        public bool bump, clicked;

        public float lastSizeBump,sizeBump,extraSizeBump,lastExtraSizeBump;

        public float flash, lastFlash;
        public bool flashBool;

        public float sin, lastSin;

        public bool greyedOut;

        ISkillTreeButton owner;

        public SkillTreeButtonBehaviour(ISkillTreeButton owner)
        {
            this.owner = owner;
        }

        public void Update()
        {
            lastCol = col;
            lastFlash = flash;
            lastSin = sin;
            lastSizeBump = sizeBump;
            flash = Custom.LerpAndTick(flash, 0f, 0.03f, 0.16666667f);

            if (owner.Selected && (!greyedOut || !owner.GetMenu.manager.menuesMouseMode))
            {
                if (!bump)
                {
                    bump = true;
                }
                sizeBump = Custom.LerpAndTick(sizeBump, 1f, 0.1f, 0.1f);
                sin += 1f;
                if (!flashBool)
                {
                    flashBool = true;
                    flash = 1f;
                }
                if (!greyedOut)
                {
                    if (owner.GetMenu.pressButton)
                    {
                        if (!clicked)
                        {
                            owner.GetMenu.PlaySound(SoundID.MENU_Button_Press_Init);
                        }
                        clicked = true;
                    }
                    if (!owner.GetMenu.holdButton)
                    {
                        if (clicked)
                        {
                            (owner as ButtonMenuObject).Clicked();
                        }
                        clicked = false;
                    }
                    col = Mathf.Min(1f, col + 0.1f);
                }
            }
            else
            {
                clicked = false;
                bump = false;
                flashBool = false;
                sizeBump = Custom.LerpAndTick(sizeBump, 0f, 0.1f, 0.05f);
                col = Mathf.Max(0f, col - 0.033333335f);
            }
            lastExtraSizeBump = extraSizeBump;
            if (bump)
            {
                extraSizeBump = Mathf.Min(1f, extraSizeBump + 0.1f);
                return;
            }
            extraSizeBump = 0f;
        }
    }

    internal interface ISkillTreeButton
    {
        bool MouseOver { get; }
        bool Selected { get; }

        Menu.Menu GetMenu { get; }
    }
}
