using RWCustom;
using TheDroneMaster.DMPS.PlayerHooks.BioReactors;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPShud.EnergyBar
{
    internal class OverDriveBar : DMPSEnergyBarBase
    {
        const int overDrivePipCount = 5;

        public float overDriveEnergy, setOverDriveEnergy;
        float overDriveEnergyPerPip, highLight, lastHighLight;

        FSprite left, right, frame, barUp, barDown;
        FSprite leftH, rightH, frameH, barUpH, barDownH;
        FSprite[] overDrivePips;

        Vector2 LeftSpriteBias => new Vector2(pipSizeFull.x * 3.5f + pipGap * 2f, 0f);
        Vector2 RightSpriteBias => new Vector2(pipSizeFull.x * 0.5f + 1f, 0f);
        Vector2 FrameSpriteBias => new Vector2(pipSizeFull.x * 0.5f, 0f);

        float BarBiasLeft => (pipSizeFull.x + pipGap) * 3f;
        float BarBiasRight => (pipSizeFull.x + pipGap) * 3f;
        float BarBiasVertical => pipSizeFull.y / 2f + 4f;

        float[] overDrivePipAlphas;

        public OverDriveBar(FContainer container) : base(container)
        {
            overDriveEnergyPerPip = OverDriveReactor.maxOverDriveCapacity / (float)overDrivePipCount;

            barUp = new FSprite("pixel")
            {
                scaleY = 3f,
                color = StaticColors.Menu.darkPink
            };
            container.AddChild(barUp);
            barUpH = new FSprite("DMPS_OverDriveBar_BarHighLight")
            {
                alpha = 0f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            container.AddChild(barUpH);

            barDown = new FSprite("pixel")
            {
                scaleY = 3f,
                color = StaticColors.Menu.darkPink
            };
            container.AddChild(barDown);
            barDownH = new FSprite("DMPS_OverDriveBar_BarHighLight")
            {
                alpha = 0f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            container.AddChild(barDownH);

            left = new FSprite("DMPS_OverDriveBar_Left", true)
            {
                anchorX = 1f,
                alpha = 1f
            };
            container.AddChild(left);
            leftH = new FSprite("DMPS_OverDriveBar_Left_HighLightMask")
            {
                anchorX = 1f,
                alpha = 0f,
                color = StaticColors.Menu.pink,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            container.AddChild(leftH);

            right = new FSprite("DMPS_OverDriveBar_Right", true)
            {
                anchorX = 1f,
                alpha = 1f
            };
            container.AddChild(right);
            rightH = new FSprite("DMPS_OverDriveBar_Right_HighLightMask")
            {
                anchorX = 1f,
                alpha = 0f,
                color = StaticColors.Menu.pink,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            container.AddChild(rightH);

            frame = new FSprite("DMPS_OverDriveBar_Frame", true)
            {
                anchorX = 0f,
                alpha = 1f
            };
            container.AddChild(frame);
            frameH = new FSprite("DMPS_OverDriveBar_Frame_HighLightMask")
            {
                anchorX = 0f,
                alpha = 0f,
                color = StaticColors.Menu.pink,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"]
            };
            container.AddChild(frameH);

            overDrivePips = new FSprite[overDrivePipCount];
            overDrivePipAlphas = new float[overDrivePipCount];

            for (int i = 0;i < overDrivePipCount; i++)
            {
                overDrivePips[i] = new FSprite("DMPS_OverDriveBar_PipHighLight", true)
                {
                    alpha = 0f,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"]
                };
                container.AddChild(overDrivePips[i]);
                overDrivePipAlphas[i] = 0f;
            }
            
        }

        public Vector2 OverDrivePipBiasX(int i)
        {
            return new Vector2(5f + i * 8f, 0f);
        }

        public Color PipColor(float percentage)
        {
            return Color.Lerp(StaticColors.Menu.pink, Color.white, Mathf.Pow(percentage, 4f));
        }

        public override void Update()
        {
            base.Update();

            lastHighLight = highLight;
            if(overDriveEnergy > 0f && highLight < 1f)
            {
                highLight += 1 / 80f;
            }
            else if(overDriveEnergy == 0f && highLight > 0f)
            {
                highLight -= 1 / 80f;
            }
            overDriveEnergy = Mathf.Lerp(overDriveEnergy, setOverDriveEnergy, Mathf.Lerp(1f, 0.25f, Mathf.Abs(overDriveEnergy - setOverDriveEnergy) * 3f));
            if(Mathf.Approximately(overDriveEnergy, setOverDriveEnergy))
                overDriveEnergy = setOverDriveEnergy;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            Vector2 leftDrawPos = LeftDrawPos(timeStacker);
            Vector2 rightDrawPos = RightDrawPos(timeStacker);

            float smoothHighLight = Mathf.Clamp01(Mathf.Lerp(lastHighLight, highLight, timeStacker) - 0.1f * Random.value);
            Color highLightColor = PipColor(smoothHighLight);

            Vector2 barPos = new Vector2((leftDrawPos.x + BarBiasLeft + rightDrawPos.x - BarBiasRight) / 2f, leftDrawPos.y);
            float barLength = Mathf.Max(0f, rightDrawPos.x - BarBiasRight - leftDrawPos.x - BarBiasLeft);

            left.SetPosition(leftDrawPos + LeftSpriteBias);
            leftH.SetPosition(leftDrawPos + LeftSpriteBias);
            leftH.alpha = smoothHighLight;

            right.SetPosition(rightDrawPos + RightSpriteBias);
            rightH.SetPosition(rightDrawPos + RightSpriteBias);
            rightH.alpha = smoothHighLight;

            frame.SetPosition(rightDrawPos + FrameSpriteBias);
            frameH.SetPosition(rightDrawPos + FrameSpriteBias);
            frameH.alpha = smoothHighLight;

            barUp.SetPosition(barPos + new Vector2(0f, BarBiasVertical));
            barUp.scaleX = barLength;
            barUpH.SetPosition(barPos + new Vector2(0f, BarBiasVertical));
            barUpH.scaleX = barLength;
            barUpH.alpha = smoothHighLight;
            barUpH.color = highLightColor;

            barDown.SetPosition(barPos + new Vector2(0f, -BarBiasVertical));
            barDown.scaleX = barLength;
            barDownH.SetPosition(barPos + new Vector2(0f, -BarBiasVertical));
            barDownH.scaleX = barLength;
            barDownH.alpha = smoothHighLight;
            barDownH.color = highLightColor;

            for (int i = 0;i < overDrivePipCount; i++)
            {
                float p = Mathf.Clamp01((overDriveEnergy - i * overDriveEnergyPerPip) / overDriveEnergyPerPip);

                overDrivePipAlphas[i] = Mathf.Lerp(overDrivePipAlphas[i], p > 0f ? 1f : 0f, 0.25f);

                overDrivePips[i].SetPosition(rightDrawPos + FrameSpriteBias + OverDrivePipBiasX(i));
                overDrivePips[i].alpha = overDrivePipAlphas[i];
                overDrivePips[i].color = PipColor(p);
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            left.RemoveFromContainer();
            right.RemoveFromContainer();
            frame.RemoveFromContainer();
            barUp.RemoveFromContainer();
            barDown.RemoveFromContainer();

            leftH.RemoveFromContainer();
            rightH.RemoveFromContainer();
            frameH.RemoveFromContainer();
            barUpH.RemoveFromContainer();
            barDownH.RemoveFromContainer();

            foreach (var s in overDrivePips)
                s.RemoveFromContainer();
        }
    }

    internal class OverDriveMessage : EnergyBarMessage
    {
        public float overDriveEnergy;
    }
}
