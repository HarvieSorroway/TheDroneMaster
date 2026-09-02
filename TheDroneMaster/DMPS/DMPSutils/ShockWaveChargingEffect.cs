using TheDroneMaster.DMPS.PlayerHooks.BioReactors;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSutils
{
    internal class ShockWaveChargingEffect : UpdatableAndDeletable, IDrawable
    {
        public static class Config
        {
            public const int CircleCount = 15;
            public static float CircleSpawnDistance = 40f;
            public static float CircleInitialRadius = 4f, CircleFinalRadius = 1.2f;
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

            public static Color CircleColor = LaserDroneGraphics.defaultLaserColor;
            public static Color BlinkColor = LaserDroneGraphics.defaultLaserColor;
            public static string LayerName = "Foreground";
        }

        private readonly Player player;
        private int age;
        private readonly float[] circleAngles;
        private readonly int[] circleAges;
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

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[Config.CircleCount];
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i] = new FSprite("Circle20") { color = Config.CircleColor, isVisible = false };
            }
            AddToContainer(sLeaser, rCam);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion) { sLeaser.CleanSpritesAndRemove(); return; }
            float progress = ChargeProgress;
            for (int i = 0; i < Config.CircleCount; i++)
            {
                if (circleAges[i] < 0) { sLeaser.sprites[i].isVisible = false; continue; }
                float circleAge = circleAges[i] + timeStacker;
                sLeaser.sprites[i].isVisible = true;
                sLeaser.sprites[i].alpha = CircleAlpha(circleAge, progress);
                sLeaser.sprites[i].scale = CircleRadius(circleAge, progress) / 10f;
                sLeaser.sprites[i].SetPosition(CirclePosition(circleAngles[i], circleAge, age + timeStacker, progress)
                    + player.DangerPos - camPos);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer = null)
        {
            if (newContainer == null) newContainer = rCam.ReturnFContainer(Config.LayerName);
            foreach (FSprite sprite in sLeaser.sprites) { sprite.RemoveFromContainer(); newContainer.AddChild(sprite); }
        }
    }
}
