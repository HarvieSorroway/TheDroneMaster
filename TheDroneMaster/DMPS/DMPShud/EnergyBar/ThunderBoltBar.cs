using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPShud.EnergyBar
{
    internal class ThunderBoltBar : DMPSEnergyBarBase
    {
        const int SmashFrames = 10;

        FSprite left, right, anvil, hammer, pole;
        FSprite smashHL, leftHL, rightHL, anvilHL, hammerHL;
        TriangleMesh rope;

        Vector2 LeftMargin => new Vector2(0f, 11.5f);

        Vector2 RightMargin => new Vector2(0f, 18.5f);

        Vector2 AnvilMargin => new Vector2(0f, 17f);

        Vector2 HammerMargin => new Vector2(4f, 17f);

        float anvilPosX;
        float displayedEnergy;

        int smash, lastSmash;

        public float prog, lastProg;

        public ThunderBoltBar(FContainer container) : base(container)
        {
            left = new FSprite("DMPS_ThunderBolt_BarLeft")
            {
                anchorX = 1f,
                anchorY = 1f
            };
            container.AddChild(left);
            leftHL = new FSprite("DMPS_ThunderBolt_BarLeft_HL")
            {
                anchorX = 1f,
                anchorY = 1f,
                color = StaticColors.defaultLaserColor,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
            };
            container.AddChild(leftHL);

            right = new FSprite("DMPS_ThunderBolt_BarRight")
            {
                anchorX = 0f,
                _anchorY = 1f
            };
            container.AddChild(right);
            rightHL = new FSprite("DMPS_ThunderBolt_BarRight_HL")
            {
                anchorX = 0f,
                _anchorY = 1f,
                color = StaticColors.defaultLaserColor,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
            };
            container.AddChild(rightHL);

            anvil = new FSprite("DMPS_ThunderBolt_PingAnvil")
            {
                anchorX = 1/33f,
                anchorY = 1f,
            };
            container.AddChild(anvil);
            anvilHL = new FSprite("DMPS_ThunderBolt_PingAnvil_HL")
            {
                anchorX = 1 / 33f,
                anchorY = 1f,
                color = StaticColors.defaultLaserColor,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
            };
            container.AddChild(anvilHL);

            hammer = new FSprite("DMPS_ThunderBolt_PingHammer")
            {
                anchorY = 0f,
                anchorX = 0.5f
            };
            container.AddChild(hammer);
            hammerHL = new FSprite("DMPS_ThunderBolt_PingHammer_HL")
            {
                anchorY = 0f,
                anchorX = 0.5f,
                color = StaticColors.defaultLaserColor,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
            };
            container.AddChild(hammerHL);

            smashHL = new FSprite("DMPS_TunderBolt_SmashLight")
            {
                anchorY = 0f,
                anchorX = 0.5f,
                color = StaticColors.defaultLaserColor,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                alpha = 0f
            };
            container.AddChild(smashHL);
        }

        public override void Update()
        {
            base.Update();
            lastProg = prog;
            lastSmash = smash;
            if (smash > 0)
                smash--;
            else
                displayedEnergy = currentEnergy;
        }

        public void Smash()
        {
            smash = lastSmash = SmashFrames;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            left.SetPosition(LeftDrawPos(timeStacker) + LeftMargin);
            leftHL.SetPosition(left.GetPosition());

            right.SetPosition(RightDrawPos(timeStacker) + RightMargin);
            rightHL.SetPosition(right.GetPosition());

            int currPip = Mathf.Max(0, Mathf.CeilToInt(displayedEnergy - 4));
            float smoothProg = Mathf.Lerp(lastProg, prog, timeStacker);
            float smoothSmash = Mathf.Lerp(lastSmash, smash, timeStacker) / SmashFrames;

            anvilPosX = currPip * pipSizeFull.x + Mathf.Max(0, currPip - 1) * pipGap;
            Vector2 anvilBasePos = LeftDrawPos(timeStacker) + new Vector2(anvilPosX, 0f);

            anvil.SetPosition(anvilBasePos + AnvilMargin);
            anvilHL.SetPosition(anvil.GetPosition());

            hammer.SetPosition(anvilBasePos + HammerMargin + Vector2.up * Mathf.Pow(smoothProg, 0.5f) * 6f);
            hammerHL.SetPosition(hammer.GetPosition());


            smashHL.SetPosition(anvilBasePos + HammerMargin);
            smashHL.alpha = smoothSmash;
            smashHL.scaleX = Mathf.Sin(Mathf.Pow(smoothSmash, 2f) * Mathf.PI);
            smashHL.scaleY = smoothSmash;

            float pwSmash = Mathf.Pow(smoothSmash, 2f);
            leftHL.alpha = rightHL.alpha = anvilHL.alpha = hammerHL.alpha = pwSmash;
        }
    }
}
