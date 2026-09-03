using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.MistTest
{
    public static class RWMistSettings
    {
        [Header("Grid")]
        [Min(2)] public static int cellsPerRoomTile = 2;
        [Min(0.001f)] public static float fixedTimeStep = 1f / 40f;
        [Range(1, 8)] public static int maximumSubsteps = 3;

        [Header("Density")]
        [Min(0f)] public static float densityDiffusion = 1.2f;
        [Min(0f)] public static float densityDissipation = 0.08f;
        [Min(0.01f)] public static float maximumDensity = 1f;
        [Range(0, 12)] public static int densityIterations = 2;

        [Header("Velocity")]
        [Min(0f)] public static float velocityDissipation = 0.15f;
        public static Vector2 ambientAcceleration = Vector2.zero;
        public static float buoyancy = 0.8f;
        public static float fogWeight = 1.1f;

        [Header("Pressure Projection")]
        [Range(1, 80)] public static int pressureIterations = 30;
        [Range(0.1f, 1f)] public static float pressureRelaxation = 1f;

        [Header("Interaction")]
        [Range(1, 1024)] public static int maximumInteractionsPerStep = 256;

        [Tooltip("交互数不超过该值时直接逐格遍历；超过后先栅格化为 BodyChunk 交互场。设为 0 可始终使用交互场。")]
        [Range(0, 64)] public static int directInteractionThreshold = 0;

        [Tooltip("BodyChunk 速度输入场影响强度的半衰期（秒）。不会让浓度源或排空效果跨帧重复。")]
        [Min(0.01f)] public static float bodyVelocityFieldHalfLife = 0.35f;

        [Header("Jet Mist")]
        [Min(1f)] public static float jetMistRadius = 18f;
        [Min(0f)] public static float jetMistDensityPerSecond = 14f;
        [Min(0f)] public static float jetMistVelocityCoupling = 18f;
        [Min(0f)] public static float jetMistVelocityMultiplier = 1.25f;
    }
}
