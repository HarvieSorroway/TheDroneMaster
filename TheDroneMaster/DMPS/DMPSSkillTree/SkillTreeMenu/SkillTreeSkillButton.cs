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
    internal class SkillTreeSkillButton : SkillTreeButton
    {
        Vector2 posFold, posExpand;
        float initScale;

        FSprite dot;

        public SkillTreeSkillButton(Menu.Menu menu, MenuObject owner, Vector2 posFold, Vector2 posExpand, string sprite, string id, float scale, bool isStatic) : base(menu, owner, posFold, sprite, id, scale, isStatic)
        {
            this.posFold = posFold;
            this.posExpand = posExpand;
            dot = new FSprite("pixel")
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                color = SkillTreeMenu.pink,
                scale = 10f * scale,
                rotation = 45f
            };
            Container.AddChild(dot);
        }

        public override void Update()
        {
            base.Update();
            pos = Vector2.Lerp(posExpand, posFold, shrink);
        }

        public override void GrafUpdate(float timeStacker)
        {
            for (int i = 0; i < subObjects.Count; i++)
            {
                subObjects[i].GrafUpdate(timeStacker);
            }

            float smoothAlpha = Mathf.Lerp(lastSetAlpha, setAlpha, timeStacker);
            float smoothScale = Mathf.Lerp(lastScale, scale, timeStacker);
            float smoothShrink = Mathf.Lerp(lastShrink, shrink, timeStacker);

            icon.SetPosition(DrawPos(timeStacker));
            icon.alpha = FlashAlpha(smoothAlpha * Mathf.Pow(1f - smoothShrink, 2f));
            icon.scale = 1.1f * smoothScale * (1f - smoothShrink);

            bkg.SetPosition(DrawPos(timeStacker));
            bkg.alpha = FlashAlpha(smoothAlpha);
            bkg.scale = smoothScale * Mathf.Lerp(80f, 18f, smoothShrink);

            dot.SetPosition(DrawPos(timeStacker));
            dot.alpha = FlashAlpha(smoothShrink * smoothAlpha);
            dot.scale = Mathf.Lerp(10f, 80f, 1f - smoothShrink);

            for (int c = 0; c < 4; c++)
            {
                float a = Mathf.PI - Mathf.PI * c / 2f;
                Vector2 anchor = DrawPos(timeStacker) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 48f * scale;

                for (int i = 0; i < 2; i++)
                {
                    Vector2 dir = Custom.DegToVec(corners[c, i].rotation);
                    corners[c, i].SetPosition(anchor - dir * 1.5f * smoothScale);
                    corners[c, i].scaleX = 3 * smoothScale;
                    corners[c, i].scaleY = Mathf.Lerp(35.5f, 20f, buttonBhv.sizeBump) * smoothScale;
                    corners[c, i].color = Color.Lerp(SkillTreeMenu.pink, Color.white, FlashAlpha(buttonBhv.sizeBump));
                    corners[c, i].alpha = FlashAlpha(smoothAlpha * Mathf.Pow(1 - smoothShrink, 4f));
                }
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            dot.RemoveFromContainer();
        }
    }
}
