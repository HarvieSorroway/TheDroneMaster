using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu
{
    internal class SkillTreeBkgEffect : PositionedMenuObject
    {
        public float progression;
        public float extraAlpha;
        List<Line> lines = new List<Line>();
        public SkillTreeBkgEffect(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            for(int i = 0; i < 10; i++)
            {
                float dProg = i / 10f;

                for(int m = 0; m < 3; m++)
                {
                   float multiDProg = m / 270f;
                    lines.Add(new Line(this, true, false, dProg + multiDProg, 1f + m / 3f));
                    lines.Add(new Line(this, true, true, dProg + multiDProg, 1f + m / 3f));
                }

                
                
                //lines.Add(new Line(this, false, false, dProg));
                //lines.Add(new Line(this, false, true, dProg));
            }
        }

        public override void Update()
        {
            base.Update();
            progression += 1 / 1600f;
            foreach (var line in lines)
                line.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float flash = (Random.value < 0.5f ? 1f : 0.8f);
            foreach (var line in lines)
                line.Draw(timeStacker, 1f);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            foreach (var line in lines)
                line.ClearSprite();
        }

        class Line
        {
            FSprite line;
            SkillTreeBkgEffect eff;
            bool horizontal;
            float reverseF,baseScale;
            float dProg;

            Vector2 pos, lastPos, halfScreenSize, screenSize;
            float scale,lastScale;
            float alpha, lastAlpha;


            public Line(SkillTreeBkgEffect eff, bool horizontal, bool reverse, float dProg, float baseScale)
            {
                this.eff = eff;
                this.horizontal = horizontal;
                this.baseScale = baseScale;
                this.dProg = dProg;

                reverseF = reverse ? 0f : 1f;

                screenSize = Custom.rainWorld.options.ScreenSize;
                halfScreenSize = screenSize / 2f;


                if (horizontal)
                {
                    line = new FSprite("DMPS_PixelGradiant20")
                    {
                        shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                        color = SkillTreeMenu.pink,
                        rotation = (reverse ? 180f : 0f),
                        scaleX = Custom.rainWorld.options.ScreenSize.x,
                        anchorY = 1f
                    };
                }
                else
                {
                    line = new FSprite("DMPS_PixelGradiant20")
                    {
                        shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                        color = SkillTreeMenu.pink,
                        rotation = 90f + (reverse ? 180f : 0f),
                        scaleY = Custom.rainWorld.options.ScreenSize.y
                    };
                }
                eff.Container.AddChild(line);

                lastPos = pos = GetPos((eff.progression + dProg) % 1);
                lastAlpha = alpha = GetAlpha((eff.progression + dProg) % 1);
            }

            public void Update()
            {
                lastPos = pos;
                lastAlpha = alpha;
                lastScale = scale;

                float t = (eff.progression + dProg) % 1;
                pos = GetPos(DMHelper.EaseInOutCubic(t));
                alpha = GetAlpha(t) * 0.3f;
                scale = GetScale(t);
            }

            public void Draw(float timeStacker, float flashAlpha)
            {
                float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                float smoothScale = Mathf.Lerp(lastScale, scale, timeStacker);
                Vector2 smoothPos = Vector2.Lerp(lastPos, pos, smoothAlpha);
                smoothPos.x = Mathf.FloorToInt(smoothPos.x);
                smoothPos.y = Mathf.FloorToInt(smoothPos.y);

                float r = Random.value;

                line.alpha = flashAlpha * 0.6f * smoothAlpha + eff.extraAlpha * smoothAlpha;
                line.color = Color.Lerp(SkillTreeMenu.pink, Color.white, r < eff.extraAlpha * smoothAlpha * 0.1f ? 0.2f : 0f);
                line.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker));

                if (horizontal)
                    line.scaleY = smoothScale;
                else
                    line.scaleX = smoothScale;
            }

            public void ClearSprite()
            {
                line.RemoveFromContainer();
            }

            Vector2 GetPos(float t)
            {
                if (horizontal)
                    return Vector2.Lerp(halfScreenSize, new Vector2(halfScreenSize.x, halfScreenSize.y * 2f * reverseF), t);
                else
                    return Vector2.Lerp(halfScreenSize, new Vector2(halfScreenSize.x * 2f * reverseF, halfScreenSize.y), t);
            }

            float GetAlpha(float t)
            {
                if (t < 0.75f)
                    return DMHelper.EaseInOutCubic(t / 0.75f);
                else
                    return DMHelper.EaseInOutCubic(1f - (t - 0.75f) * 4f);
            }

            float GetScale(float t)
            {
                return Mathf.Lerp(1f, 3f, t) * baseScale;
            }
        }
    }
}
