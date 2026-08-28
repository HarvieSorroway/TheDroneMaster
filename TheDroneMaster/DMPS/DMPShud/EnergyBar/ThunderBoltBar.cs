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
        FSprite left, right, anvil, hammer, pole;
        TriangleMesh rope;

        Vector2 LeftMargin => new Vector2(0f, 11.5f);

        Vector2 RightMargin => new Vector2(0f, 18.5f);

        Vector2 AnvilMargin => new Vector2(0f, 17f);

        Vector2 HammerMargin => new Vector2(4f, 17f);

        float anvilPosX;

        public ThunderBoltBar(FContainer container) : base(container)
        {
            left = new FSprite("DMPS_ThunderBolt_BarLeft")
            {
                anchorX = 1f,
                anchorY = 1f
            };
            container.AddChild(left);

            right = new FSprite("DMPS_ThunderBolt_BarRight")
            {
                anchorX = 0f,
                _anchorY = 1f
            };
            container.AddChild(right);

            anvil = new FSprite("DMPS_ThunderBolt_PingAnvil")
            {
                anchorX = 1/33f,
                anchorY = 1f,
            };
            container.AddChild(anvil);

            hammer = new FSprite("DMPS_ThunderBolt_PingHammer")
            {
                anchorY = 0f,
                anchorX = 0.5f
            };
            container.AddChild(hammer);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            left.SetPosition(LeftDrawPos(timeStacker) + LeftMargin);
            right.SetPosition(RightDrawPos(timeStacker) + RightMargin);

            int currPip = Mathf.Max(0, Mathf.CeilToInt(currentEnergy - 4));

            anvilPosX = currPip * pipSizeFull.x + Mathf.Max(0, currPip - 1) * pipGap;
            Vector2 anvilBasePos = LeftDrawPos(timeStacker) + new Vector2(anvilPosX, 0f);

            anvil.SetPosition(anvilBasePos + AnvilMargin);

            hammer.SetPosition(anvilBasePos + HammerMargin);
        }
    }
}
