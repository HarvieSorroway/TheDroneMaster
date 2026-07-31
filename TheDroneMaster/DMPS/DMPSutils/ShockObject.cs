using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSutils
{
    internal class ShockObject : UpdatableAndDeletable
    {
        const float airCost = 0.06f, waterCost = 0f, solidCost = 0.5f, beamCost = 0.2f, slopeCost = 0.35f;
        const int maxSplit = 5;

        Vector2 pos;
        float shootAngle, biasRange, initEnergy, splitChance;
        int seed;

        public List<ShockConnection> shocks;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShockObject"/> class.
        /// </summary>
        /// <param name="room"></param>
        /// <param name="pos">闪电激发的初始角度</param>
        /// <param name="shootAngle"></param>
        /// <param name="biasRange">闪电分裂的角度</param>
        /// <param name="initEnergy"></param>
        /// <param name="splitChance">提前分裂的概率，同时也用于控制闪电是偏向单主链还是分支链</param>
        /// <param name="falloff">闪电衰减，越接近0衰减越大</param>
        public ShockObject(Room room, Vector2 pos, float shootAngle, float biasRange, float initEnergy, float splitChance, float falloff = 1f, int seed = -1)
        {
            this.room = room;
            this.pos = pos;
            this.shootAngle = shootAngle;
            this.biasRange = biasRange;
            this.initEnergy = initEnergy;
            this.splitChance = splitChance;
            this.room.AddObject(this);

            if (seed == -1)
                seed = (int)Random.Range(0, 1145141919810);

            SpillShockPoints();
        }

        void SpillShockPoints()
        {
            var state = Random.state;
            Random.InitState(seed);

            List<UpdateInfo> updateInfos = new List<UpdateInfo>()
            {
                new UpdateInfo()
                {
                    pos = pos,
                    initAngle = shootAngle,
                    carryEnergy = initEnergy
                }
            };

            do
            {
                foreach(var info in updateInfos)
                {
                    var curr = info;
                    StepShockPoint(ref curr);

                    if(curr.carryEnergy > 0f)   //generate split shock updateinfo
                    {
                        for(int i = 0; i < maxSplit; i++)
                        {
                            if(Random.value < splitChance)
                            {
                                updateInfos.Add(new UpdateInfo()
                                {
                                    pos = curr.pos,
                                    initAngle = curr.initAngle + Random.Range(-biasRange, biasRange),
                                    carryEnergy = curr.carryEnergy * 0.5f
                                });
                            }
                        }
                    }
                }
            }
            while (updateInfos.Count > 0);

            Random.state = state;
        }

        void StepShockPoint(ref UpdateInfo info)
        {
            int iterations = Mathf.CeilToInt(info.carryEnergy);
            Vector2 dir = Custom.DegToVec(info.initAngle);
            float cost = 0f;

            for (int i = 0; i < iterations; i++)
            {
                info.pos = (i * (20f + Random.value * 20f) * dir) + info.pos;
                var tile = room.GetTile(info.pos);

                cost = 0f;

                if (tile.AnyWater) cost += waterCost;
                if(tile.Solid) cost += solidCost;
                else cost += airCost;

                if (tile.Terrain == Room.Tile.TerrainType.Slope) cost += slopeCost;
                if (tile.AnyBeam) cost += beamCost;

                info.carryEnergy -= cost;
                if (info.carryEnergy <= 0f) 
                    break;

                if (Random.value < splitChance)
                    break;
            }
        }


        struct UpdateInfo
        {
            public Vector2 pos;
            public float initAngle;
            public float carryEnergy;
        }

        public struct ShockConnection
        {
            public Vector2 posFrom, posTo;
            public float energy;
        }
    }
}
