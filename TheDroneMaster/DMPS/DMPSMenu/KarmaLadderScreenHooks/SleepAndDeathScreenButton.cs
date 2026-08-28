using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.MenuHooks.KarmaLadderScreenHooks
{
    internal class SleepAndDeathScreenButton : Menu.SimpleButton
    {
        SleepAndDeathScreen sleepAndDeathScreen;
        public SleepAndDeathScreenButton(SleepAndDeathScreen menu, MenuObject owner, string displayText, string singalText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, singalText, pos, size)
        {
            sleepAndDeathScreen = menu;
        }

        public override void Update()
        {
            base.Update();
            buttonBehav.greyedOut = sleepAndDeathScreen.ButtonsGreyedOut;
        }
    }
}
