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
    internal class SkillTreeLineRenderer : PositionedMenuObject, ISkillTreeObject
    {
        int layer;
        Vector2[] linePos;
        Vector2[] relativeMidPos, midPos;
        float[] pulseWidth; 
        FSprite[] lines;

        float shrink, lastShrink;
        SkillTreeMenu SkillMenu => menu as SkillTreeMenu;

        public SkillTreeLineRenderer(Menu.Menu menu, MenuObject owner, Vector2[] linePos, int layer) : base(menu, owner, linePos[0])
        {
            this.linePos = new Vector2[linePos.Length];
            this.layer = layer;
            for(int i = 0; i < linePos.Length; i++)//relative pos
                this.linePos[i] = linePos[i] - linePos[0];

            relativeMidPos = new Vector2[linePos.Length - 1];
            midPos = new Vector2[linePos.Length - 1];
            for (int i = 0; i < relativeMidPos.Length; i++)
            {
                relativeMidPos[i] = (linePos[i] + linePos[i + 1]) / 2f - linePos[0];
                midPos[i] = (linePos[i] + linePos[i + 1]) / 2f;
            }

            pulseWidth = new float[linePos.Length - 1];
            for (int i = 0; i < pulseWidth.Length; i++)
            {
                Vector2 rotDir = (linePos[i + 1] - linePos[i]).normalized;
                Vector2 pulseDir = (relativeMidPos[i] - SkillMenu.layerPulseCenters[layer]).normalized;
                pulseWidth[i] = Mathf.Abs(Vector2.Dot(rotDir, pulseDir)) * Vector2.Distance(linePos[i], linePos[i + 1]) + 100f;
            }

            lines = new FSprite[linePos.Length - 1];
            for(int i = 0;i < lines.Length;i++)
            {
                lines[i] = new FSprite("pixel")
                {
                    scaleX = 4f,
                    color = StaticColors.Menu.pink,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"]
                };
                Container.AddChild(lines[i]);
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            foreach (var line in lines)
                line.RemoveFromContainer();
        }

        public override void Update()
        {
            base.Update();
            lastShrink = shrink;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            float smoothShrink =(1f - Mathf.Lerp(lastShrink, shrink, timeStacker)) * 4f;
            for (int i = 0;i < lines.Length; i++)
            {
                float dist = Vector2.Distance(midPos[i], SkillMenu.layerPulseCenters[layer]);

                Vector2 rotDir = (linePos[i + 1] - linePos[i]).normalized;
                float a = Mathf.InverseLerp(0f, 1f, (SkillMenu.layerPulseRads[layer] - dist) / pulseWidth[i]);

                lines[i].rotation = Custom.VecToDeg(rotDir);
                lines[i].scaleY = (linePos[i + 1] - linePos[i]).magnitude;
                lines[i].scaleX = smoothShrink;
                lines[i].SetPosition(DrawPos(timeStacker) + relativeMidPos[i]);
                lines[i].alpha = SkillTreeButton.FlashAlpha(a);
            }
        }

        public void SetAlpha(float alpha)
        {
        }

        public void SetShrink(float shrink)
        {
            this.shrink = shrink;
        }
    }
}
