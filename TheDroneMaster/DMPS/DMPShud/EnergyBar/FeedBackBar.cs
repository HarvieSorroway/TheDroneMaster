using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPShud.EnergyBar
{
    internal class FeedBackBar : DMPSEnergyBarBase
    {
        const int fanCount = 4, pluseSplit = 20;
        const float fanRotationAngularVelPerFrame = 360f * 4f / 40f, energyToPluseIntense = 1 / 4f;

        FSprite baseFrame, frameHLMask, lineHL, fanHL, hl;
        FSprite[] fans = new FSprite[fanCount];
        TriangleMesh pluseMesh;

        //基准框覆盖整条能量条，目前以其中心为锚点对齐能量条
        static Vector2 BaseFrameBias => new Vector2(-18f, 11f);
        static Vector2 FanCenterBias = new Vector2(11f, 0f);

        static Vector2 BarTopLeft = new Vector2(-17f, 11f);
        static Vector2 BarBottomRight = new Vector2(28f, -24f);

        public float setFeedBackEfficiency = 0f;

        float fanAngle = 360f / fanCount;
        float fanRotation = Random.value * 90f;

        PluseUnit[] pluseUnits = new PluseUnit[pluseSplit];


        public FeedBackBar(FContainer container) : base(container)
        {
            for(int i = 0; i < fanCount; i++)
            {
                fans[i] = new FSprite("pixel")
                {
                    scaleX = 2f,
                    scaleY = 8f,
                    color = StaticColors.Menu.darkPink
                };
                container.AddChild(fans[i]);
            }

            fanHL = new FSprite("DMPS_FeedBackBar_Fan_Light", true)
            {
                alpha = 0f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
            };
            container.AddChild(fanHL);

            baseFrame = new FSprite("DMPS_FeedBackBar_Base", true)
            {
                anchorX = 0f,
                anchorY = 1f,
                alpha = 1f
            };
            container.AddChild(baseFrame);

            frameHLMask = new FSprite("DMPS_FeedBackBar_Mask", true)
            {
                anchorX = 0f,
                anchorY = 1f,
                alpha = 0f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                color = StaticColors.Menu.pink
            };
            container.AddChild(frameHLMask);

            lineHL = new FSprite("DMPS_FeedBackBar_Light", true)
            {
                anchorX = 0f,
                anchorY = 1f,
                alpha = 0f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                color = StaticColors.Menu.pink
            };
            container.AddChild(lineHL);

            hl = new FSprite("DMPS_JetFlare", true)
            {
                alpha = 0f,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                color = StaticColors.Menu.pink
            };
            container.AddChild(hl);

            List<TriangleMesh.Triangle> tris = new List<TriangleMesh.Triangle>();//构建脉冲条的三角形网格
            pluseUnits[0] = new PluseUnit()
            {
                indexUp = 0,
                indexDown = 1
            };
            for(int i = 1; i < pluseSplit; i++)
            {
                pluseUnits[i] = new PluseUnit()
                {
                    indexUp = i * 2,
                    indexDown = i * 2 + 1,
                };
                tris.Add(new TriangleMesh.Triangle(pluseUnits[i-1].indexUp, pluseUnits[i-1].indexDown, pluseUnits[i].indexUp));
                tris.Add(new TriangleMesh.Triangle(pluseUnits[i-1].indexDown, pluseUnits[i].indexUp, pluseUnits[i].indexDown));
            }
            pluseMesh = new TriangleMesh("DMPS_FeedBackBar_Bar", tris.ToArray(), true)
            {
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
            };
            container.AddChild(pluseMesh);

            for(int i = 0; i < pluseSplit; i++)
            {
                float uvX = i / (float)pluseSplit;
                pluseMesh.UVvertices[pluseUnits[i].indexUp] = new Vector2(uvX, 1f);
                pluseMesh.UVvertices[pluseUnits[i].indexDown] = new Vector2(uvX, 0f);

                pluseMesh.verticeColors[pluseUnits[i].indexUp] = StaticColors.Menu.pink.CloneWithNewAlpha(0f);

                pluseMesh.verticeColors[pluseUnits[i].indexDown] = StaticColors.Menu.pink.CloneWithNewAlpha(0f);
            }
            GrafUpdatePluseSprite(0f);



            pos = new Vector2(18f, 0f);
        }

        public override void Update()
        {
            base.Update();
            fanRotation = GetAngle(fanRotation + fanRotationAngularVelPerFrame * setFeedBackEfficiency);


            pluseUnits[0].lastLastPluse = pluseUnits[0].lastPluse;
            pluseUnits[0].lastPluse = pluseUnits[0].pluse;
            pluseUnits[0].pluse = Mathf.Clamp01(pluseUnits[0].pluse - 1 / 5f);
            
            for(int i = 1; i < pluseSplit; i++)
            {
                pluseUnits[i].lastLastPluse = pluseUnits[i].lastPluse;
                pluseUnits[i].lastPluse = pluseUnits[i].pluse;
                pluseUnits[i].pluse = pluseUnits[i - 1].lastPluse;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            baseFrame.SetPosition(LeftDrawPos(timeStacker) + BaseFrameBias);
            
            frameHLMask.SetPosition(LeftDrawPos(timeStacker) + BaseFrameBias); 
            frameHLMask.alpha = setFeedBackEfficiency;

            lineHL.SetPosition(LeftDrawPos(timeStacker) + BaseFrameBias);
            lineHL.alpha = setFeedBackEfficiency;

            var col = Color.Lerp(StaticColors.Menu.darkPink, StaticColors.Menu.pink, setFeedBackEfficiency);
            float smoothBaseR = GetAngle(fanRotation + fanRotationAngularVelPerFrame * setFeedBackEfficiency * timeStacker);

            for(int i = 0; i < fanCount; i++)
            {
                float r = GetAngle(smoothBaseR + fanAngle * i);
                fans[i].SetPosition(RightDrawPos(timeStacker) + FanCenterBias + Custom.DegToVec(r) * (4f + 2f));
                fans[i].rotation = r;
                fans[i].color = col;
            }

            fanHL.SetPosition(RightDrawPos(timeStacker) + FanCenterBias);
            fanHL.rotation = smoothBaseR;
            fanHL.alpha = Mathf.Pow(setFeedBackEfficiency, 1.5f);

            hl.SetPosition(RightDrawPos(timeStacker) + FanCenterBias);
            hl.alpha = Mathf.Clamp01(Mathf.Pow(setFeedBackEfficiency, 2f) - Random.value * 0.5f);

            GrafUpdatePluseSprite(timeStacker);
        }

        void GrafUpdatePluseSprite(float timeStacker)
        {
            var left = LeftDrawPos(timeStacker);
            var right = RightDrawPos(timeStacker);

            for (int i = 0; i < pluseSplit; i++)
            {
                var pluseU = pluseUnits[i];

                float f = Mathf.Lerp(pluseU.lastPluse, pluseU.pluse, timeStacker);
                //float f = .5f;
                float x = Mathf.Lerp(BarTopLeft.x + left.x, BarBottomRight.x + right.x, i / (float)pluseSplit);

                pluseMesh.verticeColors[pluseU.indexUp].a = f;
                pluseMesh.verticeColors[pluseU.indexDown].a = f;

                pluseMesh.MoveVertice(pluseU.indexUp, new Vector2(x, left.y + BarTopLeft.y));
 
                pluseMesh.MoveVertice(pluseU.indexDown, new Vector2(x, left.y + BarBottomRight.y));
            }
        }


        public override void RemoveSprites()
        {
            base.RemoveSprites();
            baseFrame.RemoveFromContainer();
            frameHLMask.RemoveFromContainer();
            lineHL.RemoveFromContainer();
            pluseMesh.RemoveFromContainer();
        }

        float GetAngle(float r)
        {
            return r % 360f;
        }

        public void Pluse(float energy)
        {
            pluseUnits[0].pluse = Mathf.Clamp01(pluseUnits[0].pluse + energy * energyToPluseIntense);
        }

        class PluseUnit
        {
            public float pluse, lastPluse, lastLastPluse;
            public int indexUp, indexDown;
        }
    }
}
