using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu
{
    internal class SkillTreeButton : PositionedMenuObject, ISkillTreeButton, SelectableMenuObject, ButtonMenuObject, ISkillTreeObject
    {
        public string id;
        public bool isStatic;
        public float scale, lastScale;

        public float setAlpha, lastSetAlpha;
        public float shrink, lastShrink;
        public Vector2? fixedPos;

        //FLabel label;
        protected FSprite icon, bkg;
        protected FSprite[,] corners;
        protected SkillTreeButtonBehaviour buttonBhv;


        public bool MouseOver => IsMouseOverMe;
        public Menu.Menu GetMenu => menu;

        public virtual bool IsMouseOverMe => Custom.DistLess(ScreenPos, menu.mousePosition, 40f) && !isStatic && setAlpha > 0f;
        public virtual bool CurrentlySelectableMouse => !buttonBhv.greyedOut && !isStatic && setAlpha > 0f;
        public virtual bool CurrentlySelectableNonMouse => !isStatic && setAlpha > 0f;

        public ButtonBehavior GetButtonBehavior => null;

        public SkillTreeButton(Menu.Menu menu, MenuObject owner, Vector2 pos, string sprite, string id, float scale, bool isStatic) : base(menu, owner, pos)
        {
            this.lastScale = this.scale = scale;
            this.isStatic = isStatic;
            this.id = id;
            setAlpha = 1f;

            shrink = lastShrink = 1f;

            buttonBhv = new SkillTreeButtonBehaviour(this);

            if (string.IsNullOrEmpty(sprite))
                sprite = "RenderNode.PlaceHolder";

            icon = new FSprite(sprite, true)
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                scale = 1.1f * scale
            };
            bkg = new FSprite("pixel", true)
            {
                color = SkillTreeMenu.pink,
                scale = 80f * scale,
                rotation = 45f
            };

            Container.AddChild(bkg);
            Container.AddChild(icon);

            corners = new FSprite[4, 2];
            for(int c = 0;c < 4;c++)
            {
                float r = 45f + c * 90f;
                for(int i =0;i < 2; i++)
                {

                    corners[c, i] = new FSprite("pixel")
                    {
                        shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                        scaleX = 3f * scale,
                        rotation = r + (i > 0 ? 0f : 90f),
                        anchorY = 0f,
                        color = SkillTreeMenu.pink,
                    };
                    Container.AddChild(corners[c, i]);
                }
            }

            page.selectables.Add(this);
        }

        public override void Update()
        {
            base.Update();
            buttonBhv.Update();
            lastSetAlpha = setAlpha;
            lastScale = scale;
            lastShrink = shrink;
            //label.text = $"{menu.mousePosition.x},{menu.mousePosition.y}\n{ScreenPos.x},{ScreenPos.y}-{MouseOver}";
        }

        public override Vector2 DrawPos(float timeStacker)
        {
            if (fixedPos != null)
                return fixedPos.Value;
            return base.DrawPos(timeStacker);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float smoothAlpha = Mathf.Lerp(lastSetAlpha, setAlpha, timeStacker);
            float smoothScale = Mathf.Lerp(lastScale, scale, timeStacker);

            icon.SetPosition(DrawPos(timeStacker));
            icon.alpha = FlashAlpha(smoothAlpha);
            icon.scale = 1.1f * smoothScale;

            bkg.SetPosition(DrawPos(timeStacker));
            bkg.alpha = FlashAlpha(smoothAlpha);
            bkg.scale = smoothScale * 80f;

            for (int c = 0;c < 4; c++)
            {
                float a = Mathf.PI - Mathf.PI * c / 2f;
                Vector2 anchor = DrawPos(timeStacker) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 48f * scale;

                for(int i = 0;i< 2; i++)
                {
                    Vector2 dir = Custom.DegToVec(corners[c, i].rotation);
                    corners[c, i].SetPosition(anchor - dir * 1.5f * smoothScale);
                    corners[c, i].scaleX = 3 * smoothScale;
                    corners[c, i].scaleY = Mathf.Lerp(35.5f, 20f, buttonBhv.sizeBump) * smoothScale;
                    corners[c, i].color = Color.Lerp(SkillTreeMenu.pink, Color.white, FlashAlpha(buttonBhv.sizeBump));
                    corners[c, i].alpha = FlashAlpha(smoothAlpha);
                }
            }
        }

        public static float FlashAlpha(float alpha)
        {
            return Random.value < alpha ? alpha : 0f;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            icon.RemoveFromContainer();
            bkg.RemoveFromContainer();
            for (int c = 0; c < 4; c++)
            {
                for (int i = 0; i < 2; i++)
                {
                    corners[c, i].RemoveFromContainer();
                }
            }
        }

        public void Clicked()
        {
            menu.Singal(this, "CLICKED");
        }

        public void SetAlpha(float alpha)
        {
            setAlpha = alpha;
        }

        public void SetShrink(float shrink)
        {
            this.shrink = shrink;
        }
    }
}
