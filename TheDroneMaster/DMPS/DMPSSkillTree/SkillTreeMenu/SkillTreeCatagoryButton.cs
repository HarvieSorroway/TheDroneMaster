using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu
{
    internal class SkillTreeCatagoryButton : SkillTreeButton
    {
        Vector2 posExpand, posFold;

        public override bool CurrentlySelectableMouse => base.CurrentlySelectableMouse && shrink == 1f;
        public override bool CurrentlySelectableNonMouse => base.CurrentlySelectableNonMouse && shrink == 1f;


        public SkillTreeCatagoryButton(Menu.Menu menu, MenuObject owner, Vector2 posFold, Vector2 posExpand, string sprite, string id, float scale, bool isStatic) : base(menu, owner, posFold, sprite, id, scale, isStatic)
        {
            this.posExpand = posExpand;
            this.posFold = posFold;
        }

        public override void Update()
        {
            base.Update();
            pos = Vector2.Lerp(posExpand, posFold, shrink);
            scale = Mathf.Lerp(1.5f, 1f, shrink);
        }
    }
}
