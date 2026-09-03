using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheDroneMaster.DMPS.PlayerHooks.BioReactors;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSutils
{
    internal class ShockWaveChargingEffect : UpdatableAndDeletable, IDrawable
    {
        public static class Config
        {
            public const int CircleCount = 0;
            public static float CircleSpawnDistance = 40f;
            public static float CircleInitialRadius = 2.5f, CircleFinalRadius = 1.2f;
            public const int Lifetime = ThunderBoltReactor.Config.ShockWaveChargeDurationFrames
                - ThunderBoltReactor.Config.ShockWaveChargeEffectSpawnTime;

            public static int CircleLifetime = 12;
            public static float CircleAppearTime = 4f;
            public static float CircleDisappearTime = 1.5f;
            public static float CircleSpawnProbability = 0.2f;
            public static float CircleMaxAlpha = 0.7f;
            public static float CircleRotationSpeed = 2.5f;
            public static float CircleJitter = 4f;
            public static float CirclePulse = 0.12f;

            public static float BlinkRadius = 40f;
            public static float BlinkMaxAlpha = 0.85f;
            public static float BlinkFrequencyStart = 0.1f;
            public static float BlinkFrequencyAcceleration = 0.04f;

            public static int NearlyCastTime = 15;
            public static float NearlyCastBlinkRadius = 240f;

            public static int ArcCount = 10, ArcNodeCount = 14;
            public static float ArcLifetime = 12f, ArcLifetimeFinal = 6.00001f;
            public static float ArcAppearProbability = 0.02f, ArcAppearProbabilityFinal = 0.2f;
            public static float CatArcProbability = 0.2f, CatArcProbabilityFinal = 0.9f;
            public static float ArcSmoothFactor = 1.5f;
            public static float ArcMaxAlpha = 1f, ArcAlphaDecayPerNode = 0.65f;
            public static float AroundingArcStartDistanceMax = 80f, AroundingArcStartDistanceMin = 60f;
            public static float AroundingArcEndDistanceMax = 40f, AroundingArcEndDistanceMin = 30f;
            public static float AroundingArcAngle = 120f, AroundingArcAngleEps = 30f;

            public static float CatArcStartDistanceMax = 100f, CatArcStartDistanceMin = 80f;
            public static float ArcDistanceAgeDecay = 0.3f;
            public static float ArcWidth = 1.5f;
            public static float ArcSpikeLengthMax = 10f, ArcSpikeLengthMin = 4f;
            public static float ArcVibrateLength = 3f;

            public static Color ArcColor = LaserDroneGraphics.defaultLaserColor;
            public static Color CircleColor = LaserDroneGraphics.defaultLaserColor;
            public static Color BlinkColor = LaserDroneGraphics.defaultLaserColor;
            public static string LayerName = "Foreground";
        }

        private readonly Player player;
        private int age;
        private readonly float[] circleAngles;
        private readonly int[] circleAges;

        int[] arcAges, arcLifetimes;
        Vector2[][] arcNodes;

        private readonly LightSource light;

        private float ChargeProgress => Mathf.Clamp01((float)age / Mathf.Max(1, Config.Lifetime));

        private static float BlinkAlpha(float age, float progress)
        {
            float phase = age * (Config.BlinkFrequencyStart
                + progress * Config.BlinkFrequencyAcceleration * age / Mathf.Max(1f, Config.Lifetime));
            return Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(0.35f, Config.BlinkMaxAlpha, progress);
        }

        private static float CircleAlpha(float circleAge, float progress)
        {
            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, Config.CircleAppearTime, circleAge));
            float fadeOut = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(
                Config.CircleLifetime - Config.CircleDisappearTime, Config.CircleLifetime, circleAge));
            float pulse = 1f + Mathf.Sin(circleAge * 0.8f) * Config.CirclePulse * progress;
            return fadeIn * fadeOut * Config.CircleMaxAlpha * pulse;
        }

        private static float CircleRadius(float circleAge, float progress)
        {
            float t = Mathf.Clamp01(circleAge / Config.CircleLifetime);
            float shrink = Mathf.SmoothStep(0f, 1f, Mathf.Pow(t, 0.8f));
            return Mathf.Lerp(Config.CircleInitialRadius, Config.CircleFinalRadius, shrink) * Mathf.Lerp(1f, 0.9f, progress);
        }

        private static Vector2 CirclePosition(float angle, float circleAge, float effectAge, float progress)
        {
            float t = Mathf.Clamp01(circleAge / Config.CircleLifetime);
            // 前段缓慢聚集，后段加速吸入中心。
            float inward = t < 0.55f ? Mathf.SmoothStep(0f, 0.42f, t / 0.55f) :
                Mathf.Lerp(0.42f, 1f, Mathf.Pow((t - 0.55f) / 0.45f, 2.2f));
            float radius = Config.CircleSpawnDistance * (1f - inward);
            float rotation = angle + effectAge * Config.CircleRotationSpeed * (0.6f + progress * 1.8f);
            float noise = (Mathf.PerlinNoise(angle * 0.01f, effectAge * 0.045f) - 0.5f) * Config.CircleJitter;
            float wobble = Mathf.Sin(effectAge * 0.16f + angle * Mathf.Deg2Rad * 2f) * Config.CircleJitter * 0.35f;
            return RWCustom.Custom.DegToVec(rotation) * (radius + noise + wobble);
        }

        public ShockWaveChargingEffect(Player player)
        {
            room = player.room;
            this.player = player;
            circleAges = new int[Config.CircleCount];
            circleAngles = new float[Config.CircleCount];
            arcAges = new int[Config.ArcCount];
            arcLifetimes = new int[Config.ArcCount];
            arcNodes = new Vector2[Config.ArcCount][];
            for (int i = 0; i < Config.ArcCount; ++i)
                arcNodes[i] = new Vector2[Config.ArcNodeCount];

            light = new LightSource(player.mainBodyChunk.pos, false, Config.BlinkColor, this);
            room.AddObject(light);
            for (int i = 0; i < Config.CircleCount; i++) circleAges[i] = -1;
        }

        public override void Update(bool eu)
        {
            if (slatedForDeletetion) return;
            float progress = ChargeProgress;
            for (int i = 0; i < Config.CircleCount; i++)
            {
                if (circleAges[i] < 0)
                {
                    if (Random.value < Config.CircleSpawnProbability * (0.75f + progress * 0.75f))
                    {
                        circleAges[i] = 0;
                        circleAngles[i] = Random.value * 360f;
                    }
                }
                else if (++circleAges[i] >= Config.CircleLifetime) circleAges[i] = -1;
            }
            for (int i = 0; i < Config.ArcCount; ++i)
            {
                float prob = Mathf.Lerp(Config.ArcAppearProbability, Config.ArcAppearProbabilityFinal, Mathf.Pow(ChargeProgress, Config.ArcSmoothFactor));
                if (arcAges[i] < 0)
                {
                    if (Random.value < prob)
                    {
                        NewArc(i);
                        arcAges[i] = 0;
                        arcLifetimes[i] = Mathf.CeilToInt(Mathf.Lerp(Config.ArcLifetime, Config.ArcLifetimeFinal, Mathf.Pow(ChargeProgress, Config.ArcSmoothFactor)));
                    }
                }
                else
                {
                    if ((arcAges[i] += 1) >= arcLifetimes[i])
                        arcAges[i] = -1;
                }
            }
            age++;
            light.setPos = player.DangerPos;
            if (Config.Lifetime - age <= Config.NearlyCastTime)
            {
                light.setRad = Mathf.Lerp(Config.BlinkRadius, Config.NearlyCastBlinkRadius,
                    1f - (Config.Lifetime - age) / (float)Config.NearlyCastTime);
            }
            else
            {
                light.setRad = Mathf.Lerp(Config.BlinkRadius * 0.7f, Config.BlinkRadius * 1.2f, progress);
            }
            light.setAlpha = BlinkAlpha(age, progress);
            if (age >= Config.Lifetime) Destroy();
        }

        private void NewArc(int i)
        {
            float sDist, eDist, sAngle, eAngle;
            float catProb = Mathf.Lerp(Config.CatArcProbability, Config.CatArcProbabilityFinal, Mathf.Pow(ChargeProgress, Config.ArcSmoothFactor));
            if (Random.value < catProb)
            {
                sDist = Random.Range(Config.CatArcStartDistanceMin, Config.CatArcStartDistanceMax);
                sDist *= Mathf.Lerp(1, Config.ArcDistanceAgeDecay, Mathf.Pow(ChargeProgress, Config.ArcSmoothFactor));
                eDist = 5f;
                sAngle = Random.value * 360;
                eAngle = Random.value * 360;
            } else
            {
                sDist = Random.Range(Config.AroundingArcStartDistanceMin, Config.AroundingArcStartDistanceMax);
                eDist = Random.Range(Config.AroundingArcEndDistanceMin, Config.AroundingArcEndDistanceMax);
                sDist *= Mathf.Lerp(1, Config.ArcDistanceAgeDecay, Mathf.Pow(ChargeProgress, Config.ArcSmoothFactor));
                eDist *= Mathf.Lerp(1, Config.ArcDistanceAgeDecay, Mathf.Pow(ChargeProgress, Config.ArcSmoothFactor));
                sAngle = Random.value * 360;
                float angleEps = Config.AroundingArcAngle + Random.Range(-Config.AroundingArcAngleEps, Config.AroundingArcAngleEps);
                eAngle = sAngle + angleEps * Mathf.Sign(Random.value - 0.5f);
            }
            static Vector2 PolarLerp(Vector2 a, Vector2 b, float t)
            {
                Vector2 aPolar = new Vector2(a.magnitude, RWCustom.Custom.VecToDeg(a));
                Vector2 bPolar = new Vector2(b.magnitude, RWCustom.Custom.VecToDeg(b));
                Vector2 cPolar = new Vector2(
                    Mathf.Lerp(aPolar.x, bPolar.x, t),
                    Mathf.LerpAngle(aPolar.y, bPolar.y, t));
                return RWCustom.Custom.DegToVec(cPolar.y) * cPolar.x;
            }
            arcNodes[i][0] = sDist * RWCustom.Custom.DegToVec(sAngle);
            arcNodes[i][Config.ArcNodeCount - 1] = eDist * RWCustom.Custom.DegToVec(eAngle);
            Queue<System.ValueTuple<int, int>> queue = new();
            queue.Enqueue((0, Config.ArcNodeCount - 1));
            while (queue.Count > 0)
            {
                var (l, r) = queue.Dequeue();
                if (r == l + 1)
                    continue;
                int m = (r + l) / 2;
                float lerp = (m - l + 0f) / (r - l);
                float spkLen = Mathf.Lerp(Config.ArcSpikeLengthMax, Config.ArcSpikeLengthMin, m * 1.0f / (Config.ArcNodeCount - 1));
                Vector2 perp = RWCustom.Custom.PerpendicularVector(arcNodes[i][r] - arcNodes[i][l]);
                Vector2 mid = PolarLerp(arcNodes[i][l], arcNodes[i][r], lerp);
                mid += perp * Mathf.Sign(Random.value - 0.5f) * spkLen * Mathf.Lerp(0.4f, 1, Random.value);
                arcNodes[i][m] = mid;
                queue.Enqueue((l, m));
                queue.Enqueue((m, r));
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[Config.CircleCount + Config.ArcCount];
            for (int i = 0; i < Config.CircleCount; i++)
            {
                sLeaser.sprites[i] = new FSprite("Circle20") { color = Config.CircleColor, isVisible = false };
            }
            for (int i = 0; i < Config.ArcCount; ++i)
            {
                sLeaser.sprites[Config.CircleCount + i] = TriangleMesh.MakeLongMesh(Config.ArcNodeCount - 1, false, true);
            }
            AddToContainer(sLeaser, rCam);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion) { sLeaser.CleanSpritesAndRemove(); return; }
            float progress = ChargeProgress;
            Vector2 playerPos = player.DangerPos;
            for (int i = 0; i < Config.CircleCount; i++)
            {
                if (circleAges[i] < 0) { sLeaser.sprites[i].isVisible = false; continue; }
                float circleAge = circleAges[i] + timeStacker;
                sLeaser.sprites[i].isVisible = true;
                sLeaser.sprites[i].alpha = CircleAlpha(circleAge, progress);
                sLeaser.sprites[i].scale = CircleRadius(circleAge, progress) / 10f;
                sLeaser.sprites[i].SetPosition(CirclePosition(circleAngles[i], circleAge, age + timeStacker, progress)
                    + playerPos - camPos);
            }
            for (int i = 0; i < Config.ArcCount; ++i)
            {
                int si = i + Config.CircleCount;
                if (arcAges[i] < 0)
                {
                    sLeaser.sprites[si].isVisible = false;
                    continue;
                }
                sLeaser.sprites[si].isVisible = true;
                TriangleMesh trig = sLeaser.sprites[si] as TriangleMesh;
                float[] alpha = new float[Config.ArcNodeCount];
                float agex = (arcAges[i] + timeStacker) / arcLifetimes[i] * (Config.ArcNodeCount - 1);
                Vector2[] vibrate = new Vector2[Config.ArcNodeCount];
                for (int j = 0; j < Config.ArcNodeCount; ++j)
                {
                    alpha[j] = Mathf.Pow(Config.ArcAlphaDecayPerNode, Mathf.Abs(j - agex)) * Config.ArcMaxAlpha;
                    vibrate[j] = Random.insideUnitCircle * Config.ArcVibrateLength * Random.value;
                }
                for (int j = 0; j < Config.ArcNodeCount - 1; ++j)
                {
                    int vertOffset = j * 4;
                    Vector2 d = RWCustom.Custom.PerpendicularVector(arcNodes[i][j + 1] - arcNodes[i][j]);
                    trig.MoveVertice(vertOffset + 0, arcNodes[i][j] + vibrate[j] + d * Config.ArcWidth / 2f + playerPos - camPos);
                    trig.MoveVertice(vertOffset + 1, arcNodes[i][j] + vibrate[j] - d * Config.ArcWidth / 2f + playerPos - camPos);
                    trig.MoveVertice(vertOffset + 2, arcNodes[i][j + 1] + vibrate[j + 1] + d * Config.ArcWidth / 2f + playerPos - camPos);
                    trig.MoveVertice(vertOffset + 3, arcNodes[i][j + 1] + vibrate[j + 1] - d * Config.ArcWidth / 2f + playerPos - camPos);
                    trig.verticeColors[vertOffset + 0] = Config.ArcColor.CloneWithMultipliedAlpha(alpha[j]);
                    trig.verticeColors[vertOffset + 1] = Config.ArcColor.CloneWithMultipliedAlpha(alpha[j]);
                    trig.verticeColors[vertOffset + 2] = Config.ArcColor.CloneWithMultipliedAlpha(alpha[j + 1]);
                    trig.verticeColors[vertOffset + 3] = Config.ArcColor.CloneWithMultipliedAlpha(alpha[j + 1]);
                }
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer = null)
        {
            if (newContainer == null) newContainer = rCam.ReturnFContainer(Config.LayerName);
            foreach (FSprite sprite in sLeaser.sprites)
                sprite.RemoveFromContainer();
            foreach (FSprite fsp in sLeaser.sprites)
                newContainer.AddChild(fsp);
        }
    }
}
