using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.MistTest
{
    public sealed class RWMistPhysicalObjectProfile
    {
        [Tooltip("用于雾气交互范围的 BodyChunk 半径倍率。")]
        [Min(0.01f)] public float radiusMultiplier = 1f;

        [Tooltip("额外的交互半径，单位为雨世界世界像素。")]
        [Min(0f)] public float radiusPadding = 4f;

        [Tooltip("周围雾气跟随 BodyChunk 速度的快慢；0 表示不传递速度。")]
        [Min(0f)] public float velocityCoupling = 12f;

        [Tooltip("向外推开雾气并移除 BodyChunk 内部浓度；0 表示不排开。")]
        [Min(0f)] public float displacement = 5f;

        [Tooltip("每秒产生的浓度；负值表示吸收雾气。")]
        public float densityPerSecond;
    }
}
