using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.PlayerHooks
{
    internal class DMPSBioReactorGraphics
    {
        public static readonly float reactorBias = 4f;

        public PlayerGraphics pGraphics;
        Color fullEnergyCol = LaserDroneGraphics.defaultLaserColor;
        Color lowEnergyCol = Color.cyan;

        public int startIndex;
        public int totalSprite => 3;
        public float energy;

        public DMPSBioReactorGraphics(PlayerGraphics pGraphics, Color fullEnergyCol, int startIndex)
        {
            this.pGraphics = pGraphics;
            this.fullEnergyCol = fullEnergyCol;
            this.startIndex = startIndex;
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[startIndex] = new FSprite("DMPS_BioReactor", true)
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                color = fullEnergyCol,
                scale = 0.5f,
                alpha = 0.5f
            };
            sLeaser.sprites[startIndex + 1] = new FSprite("DMPS_BioReactorFlare", true)
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                color = fullEnergyCol,
                alpha = 0.5f
            };
            sLeaser.sprites[startIndex + 2] = new FSprite("DMPS_BioReactorFlare", true)
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                color = fullEnergyCol,
                alpha = 0.5f
            };
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            var water = rCam.ReturnFContainer("Water");
            water.AddChild(sLeaser.sprites[startIndex]);
            water.AddChild(sLeaser.sprites[startIndex + 1]);
            water.AddChild(sLeaser.sprites[startIndex + 2]);
        }
        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[startIndex].color = fullEnergyCol;
            sLeaser.sprites[startIndex + 1].color = fullEnergyCol;
            sLeaser.sprites[startIndex + 2].color = fullEnergyCol;
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(pGraphics.drawPositions[0, 1], pGraphics.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(pGraphics.drawPositions[1, 1], pGraphics.drawPositions[1, 0], timeStacker);

            float rotation = Custom.AimFromOneVectorToAnother(vector2, vector);
            Vector2 dir = Custom.DirVec(vector2, vector);

            Vector2 perDir = Custom.PerpendicularVector(dir) * Custom.LerpMap(Mathf.Abs(Mathf.Abs(rotation) - 90f), 90f, 60f, 0f, 1f) * reactorBias * (rotation > 0 ? 1f : -1f);
            Color color = Color.Lerp(lowEnergyCol, fullEnergyCol, energy);

            float f = Random.value;
            Vector2 pos = vector - camPos - perDir - dir * 7f;
            sLeaser.sprites[startIndex].SetPosition(pos);
            sLeaser.sprites[startIndex].alpha = 0.2f + f * 0.5f;
            sLeaser.sprites[startIndex].scale = 0.3f + 0.2f * f;
            sLeaser.sprites[startIndex].color = color;


            float flareF = Mathf.InverseLerp(0f, Custom.rainWorld.options.ScreenSize.x, pos.x) * 2f - 1f;
            float flareX = 40f * flareF;

            float flareAlpha = (1f - Mathf.Pow(1f - Mathf.Abs(flareF), 2f)) * (0.2f + f * 0.5f);
            float flareScale = flareAlpha * 0.5f + 0.5f;

            sLeaser.sprites[startIndex + 1].SetPosition(pos + new Vector2(flareX, -3f));
            sLeaser.sprites[startIndex + 1].alpha = flareAlpha;
            sLeaser.sprites[startIndex + 1].scaleX = flareScale;
            sLeaser.sprites[startIndex + 1].color = color;

            sLeaser.sprites[startIndex + 2].SetPosition(pos + new Vector2(-flareX, 3f));
            sLeaser.sprites[startIndex + 2].alpha = flareAlpha;
            sLeaser.sprites[startIndex + 2].scaleX = flareScale;
            sLeaser.sprites[startIndex + 2].color = color;
        }

        public void Update()
        {

        }
    }
}
