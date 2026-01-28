using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu.MenuAnim
{
    internal class UnfocusingCatagoryAnim : MenuAnimation
    {
        public override bool AllowSignal => progression > 0.5f;
        public UnfocusingCatagoryAnim(SkillTreeMenu menu) : base(menu, 60)
        {
            menu.skillInfoScreen.SetFocusingSkillNode(string.Empty, "RenderNode.PlaceHolder");
        }

        public override void Update()
        {
            base.Update();
            float focusingAnimProg = 1f - Mathf.Clamp01(progression * 2f - 0.2f);
            float lstProg = 1f - Mathf.Clamp01(lastProgression * 2f - 0.2f);
            float t = DMHelper.EaseInOutCubic(focusingAnimProg);

            foreach (var id in menu.layerNodeLst[0])
                menu.idMapper[id].SetAlpha(1f - t);
            foreach (var id in menu.layerNodeLst[1])
            {
                if (id != menu.focusingSkillTreeCatagory)
                    menu.idMapper[id].SetAlpha(1f - t);
                else
                    menu.idMapper[id].SetShrink(1f - t);
            }
            foreach (var subNode in RenderNodeLoader.idMapper[menu.focusingSkillTreeCatagory].subRenderNodeInfo)
            {
                if (menu.groupIdMapper.TryGetValue(subNode, out var lst))
                {
                    foreach (var id in lst)
                    {
                        if (menu.idMapper.TryGetValue(id, out var obj))
                            obj.SetShrink(1f - t);
                    }
                }
            }

            menu.bkgEff.progression += (DMHelper.EaseInOutCubic(focusingAnimProg) - DMHelper.EaseInOutCubic(lstProg)) * 0.3f;
            menu.bkgEff.extraAlpha = Mathf.Sin(Mathf.PI * focusingAnimProg);

            menu.escButton.SetAlpha(t);
            menu.pages[0].pos = Vector2.Lerp(menu.middleScreen, Vector2.zero, t);

            if (progression < 0.25f)
                menu.layerPulseRads[2] = (1f - progression / 0.25f) * 2000f;
            else
                menu.layerPulseRads[2] = 0f;

            if (progression > 0.5f)
                menu.layerPulseRads[0] = (progression * 2f - 1f) * 800f;
            else
                menu.layerPulseRads[0] = 0f;
        }

        public override void Finish()
        {
            menu.focusingSkillTreeCatagory = string.Empty;
        }
    }
}
