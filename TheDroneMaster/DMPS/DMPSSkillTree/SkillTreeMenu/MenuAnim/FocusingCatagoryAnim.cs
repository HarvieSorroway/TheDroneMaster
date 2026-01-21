using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu.MenuAnim
{
    internal class FocusingCatagoryAnim : MenuAnimation
    {
        public FocusingCatagoryAnim(SkillTreeMenu menu) : base(menu, 90)
        {
        }

        public override void Update()
        {
            base.Update();
            float focusingAnimProg = Mathf.Clamp01(progression * 3f);
            float lastFocusingAnimProg = Mathf.Clamp01(lastProgression * 3f);
            float t = DMHelper.EaseInOutCubic(focusingAnimProg);

            foreach (var id in menu.layerNodeLst[0])
            {
                menu.idMapper[id].SetAlpha(1f - t);
            }
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
            menu.bkgEff.progression += (DMHelper.EaseInOutCubic(focusingAnimProg) - DMHelper.EaseInOutCubic(lastFocusingAnimProg)) * 0.3f;
            menu.bkgEff.extraAlpha = Mathf.Sin(Mathf.PI * focusingAnimProg);

            menu.escButton.SetAlpha(focusingAnimProg);
            menu.pages[0].pos = Vector2.Lerp(menu.middleScreen, Vector2.zero, t);

            if (progression < 0.33f)
                menu.layerPulseRads[0] = (1f - progression * 3f) * 800f;
            else
                menu.layerPulseRads[0] = 0f;

            if (progression > 0.33f)
                menu.layerPulseRads[2] = (progression - 0.33f) / 0.66f * 2000f;
            else
                menu.layerPulseRads[2] = 0f;
        }
    }
}
