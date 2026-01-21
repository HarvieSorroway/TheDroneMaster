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
    internal class SkillTreeHoldButton : PositionedMenuObject, ISkillTreeObject, SelectableMenuObject, ButtonMenuObject, ISkillTreeButton
    {
        Vector2 scale;

        public float setAlpha, lastSetAlpha;
        public float prog, lastProg;

        protected FLabel label;
        protected FSprite fill, bkg;
        protected SkillTreeButtonBehaviour buttonBhv;

        public bool MouseOver => IsMouseOverMe;
        public Menu.Menu GetMenu => menu;
        public virtual bool IsMouseOverMe
        {
            get
            {
                Vector2 local = menu.mousePosition - ScreenPos;
                bool res = false;
                if (local.x < scale.x / 2f && local.x > -scale.x / 2f && local.y > -scale.y / 2f && local.y < scale.y / 2f)
                    res = true;
                res = res && setAlpha > 0f;
                return res;
            }
        }
        public virtual bool CurrentlySelectableMouse => !buttonBhv.greyedOut && setAlpha > 0f;
        public virtual bool CurrentlySelectableNonMouse => setAlpha > 0f;
        public ButtonBehavior GetButtonBehavior => null;


        public SkillTreeHoldButton(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 scale, string displayText, Action callBack) : base(menu, owner, pos)
        {
            setAlpha = 1f;
            buttonBhv = new SkillTreeButtonBehaviour(this);
            this.pos = pos;
            this.scale = scale;

            bkg = new FSprite("pixel", true)
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                scaleX = scale.x,
                scaleY = scale.y,
                alpha = 1f,
                color = SkillTreeMenu.pink
            };
            Container.AddChild(bkg);

            fill = new FSprite("pixel", true)
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                scaleY = scale.y,
                alpha = 0f,
                color = SkillTreeMenu.pink * 0.5f + Color.white * 0.5f
            };
            Container.AddChild(fill);
            label = new FLabel(Custom.GetDisplayFont(), "")
            ;
            Container.AddChild(label);
        }

        public override void Update()
        {
            base.Update();
            buttonBhv.Update();
            lastSetAlpha = setAlpha;
            lastProg = prog;

            if(Selected && !buttonBhv.greyedOut && menu.holdButton)
            {
                prog = Mathf.Clamp01(prog + 1 / 40f);
            }
            else if(prog > 0f)
            {
                prog = Mathf.Clamp01(prog - 1 / 20f);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float smoothAlpha = Mathf.Lerp(lastSetAlpha, setAlpha, timeStacker);
            float smoothProg = Mathf.Lerp(lastProg, prog, timeStacker);

            bkg.SetPosition(DrawPos(timeStacker));

            float scaleX = Mathf.Pow(smoothProg, 0.5f) * bkg.scaleX;
            fill.scaleX = scaleX;
            fill.SetPosition(DrawPos(timeStacker) + new Vector2(scaleX * 0.5f - scale.x / 2f, 0f));
            fill.alpha = Mathf.Pow(smoothProg, 0.5f);

            label.SetPosition(DrawPos(timeStacker));
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
        }

        public void Clicked()
        {
            //pass
        }

        public void SetAlpha(float alpha)
        {
            setAlpha = alpha;
            prog = lastProg = 0f;
        }

        public void SetShrink(float shrink)
        {
        }
    }
}
