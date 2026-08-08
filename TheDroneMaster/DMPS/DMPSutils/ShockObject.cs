using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.ParticleSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSutils
{
    internal class ShockObject : UpdatableAndDeletable, IDrawable
    {
        const float airCost = 0.15f, waterCost = 0f, solidCost = 1.5f, beamCost = 0.5f, slopeCost = 0.65f;
        const int maxSplit = 5, maxLife = 40, maxSteps = 5;


        Vector2 pos;
        float shootAngle, biasRange, initEnergy, splitChance;
        int _seed = -1;

        //interation
        Random.State _state;
        List<UpdateInfo> updateInfos, updateInfosNext;
        float mainBranchExtSplitChance = 0f;
        bool mainBranchSplited = false;

        //render
        FContainer _container;
        List<ShockConnection> shocks = new List<ShockConnection>();

        int life = 0, lastLife = 0;

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
            _seed = seed;
            _seed = (int)Random.Range(0, 114514);

            Init();
        }

        void Init()
        {
            var state = Random.state;
            Random.InitState(_seed);
            _state = Random.state;
            Random.state = state;

            updateInfos = new List<UpdateInfo>()
            {
                new UpdateInfo()
                {
                    pos = pos,
                    initAngle = shootAngle,
                    carryEnergy = initEnergy,
                    splits = 3
                }
            };
            updateInfosNext = new List<UpdateInfo>();
        }

        void SpillShockPointsUpdate()
        {
            var state = Random.state;
            Random.state = _state;

            int steps = 0;
            do
            {
                for(int i = updateInfos.Count - 1; i >= 0; i--)
                {
                    var curr = updateInfos[i];
                    bool split = StepShockPoint(ref curr, ref steps);

                    if (curr.carryEnergy > 0.05f)
                    {
                        if (split)
                        {
                            if (Random.value < splitChance)//分裂闪电
                            {
                                float nextSplitChance = 1f;
                                for (int j = 0; j < curr.splits; j++)
                                {
                                    nextSplitChance *= Mathf.Lerp(splitChance, 1f, Mathf.Pow(Random.value, 2f));//更小分支概率
                                    if (Random.value < splitChance)
                                    {
                                        updateInfosNext.Add(new UpdateInfo()
                                        {
                                            pos = curr.pos,
                                            initAngle = curr.initAngle + Random.Range(-biasRange, biasRange),
                                            carryEnergy = curr.carryEnergy * 0.5f,
                                            splits = curr.splits - 1,
                                        });
                                    }
                                    else
                                        break;
                                }
                            }
                            curr.splits -= 1;
                        }
                        //主链闪电

                        float angle = curr.initAngle + Random.Range(-biasRange, biasRange) * Mathf.Lerp(splitChance, 1f, Random.value * 0.5f) * (Mathf.Clamp(1f / (curr.carryEnergy * 0.25f + 0.1f), 0.2f, 1f));

                        while (angle < shootAngle - 90) { angle += 45f; }
                        while (angle > shootAngle + 90) { angle -= 45f; }

                        updateInfosNext.Add(new UpdateInfo()
                        {
                            pos = curr.pos,
                            initAngle = angle,
                            carryEnergy = curr.carryEnergy,
                            splits = curr.splits
                        });
                    }
 
                    updateInfos.RemoveAt(i);
                    if (steps >= maxSteps)
                        break;
                }
                updateInfos.AddRange(updateInfosNext);
                updateInfosNext.Clear();
            }
            while (updateInfos.Count > 0 && steps < maxSteps);

            _state = Random.state;
            Random.state = state;
        }

        bool StepShockPoint(ref UpdateInfo info, ref int steps)
        {
            bool split = false;
            ShockConnection connection = new ShockConnection()
            {
                posFrom = info.pos,
                energy = info.carryEnergy,
                info = info.initAngle.ToString()
            };

            int iterations = Mathf.CeilToInt(info.carryEnergy);
            Vector2 dir = Custom.DegToVec(info.initAngle);
            float cost = 0f;

            //Plugin.Log($"Step for info : {index},  pos start:{info.pos}, e start:{info.carryEnergy}, e angle:{info.initAngle}");

            for (int i = 0; i < iterations; i++)
            {
                steps++;
                //Plugin.Log($"Step : {index} - {i}, pos:{info.pos}, e:{info.carryEnergy}");
                info.pos = (10f + Random.value * 10f) * dir + info.pos;
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

                if ((Random.value < Mathf.Lerp(splitChance, 1f, 1f - Mathf.Clamp01(1f/info.carryEnergy)) + mainBranchExtSplitChance) && info.splits > 0)//提高前期分支的分裂概率
                {
                    split = true;
                    mainBranchSplited = true;
                    mainBranchExtSplitChance = 0f;
                }

                if (!split && !mainBranchSplited)
                    mainBranchExtSplitChance += 0.55f;

                if ((Random.value < 0.5f || split) && info.carryEnergy > 0.1f)//弯折闪电链
                {
                    break;
                }
            }
            Plugin.Log("");

            connection.posTo = info.pos;
            
            if(_container != null)
            {
                connection.CreateSprite();
                connection.AddSprites(_container);
            }

            shocks.Add(connection);

            return split;
        }

        public override void Update(bool eu)
        {
            if (slatedForDeletetion)
                return;

            lastLife = life;
            if (life < maxLife)
            {
                life++;
            }
            else
                Destroy();

            if (updateInfos.Count > 0)
            {
                SpillShockPointsUpdate();
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            _container = new FContainer();
            foreach (var s in shocks)
                s.AddSprites(_container);
            AddToContainer(sLeaser, rCam, null);
            //rCam.room.PlaySound(DMEnums.DMPS.Sound.DMPS_ShootFuse,pos, 0.1f, 1f);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
                _container.RemoveFromContainer();
                foreach (var s in shocks)
                    s.RemoveSprites();
                shocks.Clear();
                if(!slatedForDeletetion)
                    Destroy();
            }

            float smoothF = Mathf.Lerp((float)lastLife, (float)life, timeStacker) / (float)maxLife;
            float smoothWidth = 1f - DMHelper.EaseInOutCubic(smoothF);
            float flashFactor = Mathf.Sin(Mathf.Clamp01(smoothF * 6) * Mathf.PI) * 0.5f;

            foreach(var s in shocks)
            {
                s.Draw(rCam, timeStacker, camPos, smoothWidth, flashFactor);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Water").AddChild(_container);
        }



        struct UpdateInfo
        {
            public Vector2 pos;
            public float initAngle;
            public float carryEnergy;
            public int splits;
        }

        public class ShockConnection
        {
            const float minScale = 1f, maxScale = 3.5f;

            public Vector2 posFrom, posTo;
            public float energy;
            public string info;

            public FSprite sprite, gradiantA, gradiantB;
            float scaleX, gradiantScaleY;

            public void CreateSprite()
            {
                if (sprite == null)
                {
                    scaleX = Mathf.Lerp(minScale, maxScale, Mathf.InverseLerp(0f, 5f, energy));
                    gradiantScaleY = Mathf.Lerp(1.8f, 3.2f, Mathf.InverseLerp(minScale, maxScale, scaleX));

                    sprite = new FSprite("pixel")
                    {
                        color = LaserDroneGraphics.defaultLaserColor,
                        rotation = Custom.AimFromOneVectorToAnother(posFrom, posTo),
                        scaleX = scaleX,
                        scaleY = (posTo - posFrom).magnitude,
                        shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    };

                    gradiantA = new FSprite("DMPS_PixelGradiant20")
                    {
                        color = LaserDroneGraphics.defaultLaserColor,
                        rotation = Custom.AimFromOneVectorToAnother(posFrom, posTo) + 90f,
                        scaleX = (posTo - posFrom).magnitude,
                        scaleY = 0f,
                        anchorY = 1f,
                        shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    };

                    gradiantB = new FSprite("DMPS_PixelGradiant20")
                    {
                        color = LaserDroneGraphics.defaultLaserColor,
                        rotation = Custom.AimFromOneVectorToAnother(posFrom, posTo) + 180f + 90f,
                        scaleX = (posTo - posFrom).magnitude,
                        scaleY = 0f,
                        anchorY = 1f,
                        shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    };
                }
            }

            public void AddSprites(FContainer container)
            {
                container.AddChild(sprite);
                container.AddChild(gradiantA);
                container.AddChild(gradiantB);
            }

            public void RemoveSprites()
            {
                sprite?.RemoveFromContainer();
                gradiantA?.RemoveFromContainer();
                gradiantB?.RemoveFromContainer();
            }

            public void Draw(RoomCamera rCam, float timeStacker, Vector2 camPos, float widthFactor, float flashFactor)
            {
                Vector2 pos = (posFrom + posTo) * 0.5f - camPos;
                sprite.SetPosition(pos);
                gradiantA.SetPosition(pos); 
                gradiantB.SetPosition(pos);

                sprite.scaleX = scaleX * (widthFactor + flashFactor);
                sprite.alpha = widthFactor;
                sprite.color = Color.Lerp(Color.white, LaserDroneGraphics.defaultLaserColor, widthFactor * 4f);

                gradiantA.scaleY = gradiantScaleY * flashFactor + widthFactor * 0.2f;
                gradiantA.alpha = flashFactor * widthFactor;

                gradiantB.scaleY = gradiantScaleY * flashFactor + widthFactor * 0.2f;
                gradiantB.alpha = flashFactor * widthFactor;
            }
        }
    }
}
