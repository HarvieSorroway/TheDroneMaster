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
        static int maxFlashCounter = 80;

        public string id;

        int layer;
        Vector2[] linePos;
        Vector2[] relativeMidPos, midPos;
        float[] pulseWidth; 
        FSprite[] lines;

        float shrink, lastShrink, setShrink, modeShrink, lastModeShrink, setModeShrink;
        SkillTreeMenu SkillMenu => menu as SkillTreeMenu;

        LineRenderMode _renderMode;
        public LineRenderMode RenderMode
        {
            get => _renderMode;
            set
            {
                _renderMode = value;
                switch (_renderMode)
                {
                    case LineRenderMode.Preview:
                        setModeShrink = 0.5f;
                        break;
                    case LineRenderMode.Show:
                        setModeShrink = 1f;
                        break;
                    case LineRenderMode.Hide:
                        setModeShrink = 0f;
                        break;
                }

                //Plugin.Log($"{id} set mode to {_renderMode}");
            }
        }

        int flashCounter;
        float previewFlash = 1f, lastPreviewFlash = 1f;

        public SkillTreeLineRenderer(Menu.Menu menu, MenuObject owner, Vector2[] linePos, int layer, string id) : base(menu, owner, linePos[0])
        {
            shrink = lastShrink = setShrink = 1f;
            this.linePos = new Vector2[linePos.Length];
            this.layer = layer;
            this.id = id;
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
            lastModeShrink = modeShrink;

            shrink = Mathf.Lerp(shrink, setShrink, 0.25f);
            modeShrink = Mathf.Lerp(modeShrink, setModeShrink, 0.25f);

            lastPreviewFlash = previewFlash;
            if(RenderMode == LineRenderMode.Preview)
            {
                flashCounter++;
                if (flashCounter > maxFlashCounter)
                    flashCounter = 0;

                previewFlash = 1f - Mathf.InverseLerp(0f, maxFlashCounter, flashCounter);
            }
            else
            {
                flashCounter = 0;
                previewFlash = 1f;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            float smoothShrink = (1f - Mathf.Lerp(lastShrink, shrink, timeStacker)) * 4f;
            float smoothModeShrink = Mathf.Lerp(lastModeShrink, modeShrink, timeStacker);
            float smoothPreviewFlash = Mathf.Lerp(lastPreviewFlash, previewFlash, timeStacker);

            for (int i = 0;i < lines.Length; i++)
            {
                float dist = Vector2.Distance(midPos[i], SkillMenu.layerPulseCenters[layer]);

                Vector2 rotDir = (linePos[i + 1] - linePos[i]).normalized;
                float a = Mathf.InverseLerp(0f, 1f, (SkillMenu.layerPulseRads[layer] - dist) / pulseWidth[i]) * smoothPreviewFlash;

                lines[i].rotation = Custom.VecToDeg(rotDir);
                lines[i].scaleY = (linePos[i + 1] - linePos[i]).magnitude;
                lines[i].scaleX = smoothShrink * smoothModeShrink;
                lines[i].SetPosition(DrawPos(timeStacker) + relativeMidPos[i]);
                lines[i].alpha = SkillTreeButton.FlashAlpha(a);
            }
        }

        public void SetAlpha(float alpha)
        {
        }

        public void SetShrink(float shrink)
        {
            this.setShrink = shrink;
        }

        public enum LineRenderMode
        {
            Preview,
            Show,
            Hide
        }
    }
}
