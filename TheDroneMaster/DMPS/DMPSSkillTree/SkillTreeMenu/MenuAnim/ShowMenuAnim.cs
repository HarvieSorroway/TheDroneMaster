using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu.MenuAnim
{
    internal class ShowMenuAnim : MenuAnimation
    {
        public ShowMenuAnim(SkillTreeMenu menu) : base(menu, 40)
        {
        }

        public override void Update()
        {
            base.Update();
            menu.layerPulseRads[0] = DMHelper.LerpEase(progression) * 800f;
        }
    }
}
