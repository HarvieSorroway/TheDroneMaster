using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu.MenuAnim
{
    internal class QuitMenuAnim : MenuAnimation
    {
        public QuitMenuAnim(SkillTreeMenu menu) : base(menu, 40)
        {
        }

        public override void Update()
        {
            base.Update();
            base.Update();
            menu.layerPulseRads[0] = (1f - DMHelper.LerpEase(progression)) * 800f;
            menu.alpha = 1f - DMHelper.EaseInOutCubic(progression);
        }

        public override void Finish()
        {
            base.Finish();
            menu.ShutDownProcess();
        }
    }
}
