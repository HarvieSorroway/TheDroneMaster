using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using TheDroneMaster.DMPS.DMPSDrone;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu.MenuAnim;
using TheDroneMaster.DMPS.PlayerHooks.BioReactors;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSutils
{
    internal class ShockWaveObject : UpdatableAndDeletable, IDrawable
    {
        // 瞬间伤害和击晕
        private const float damage = 1f;
        private const int shockStun = 80;
        
        private const float tileSize = 20f;
        private const float diagonalLength = 1.41421356f;

        // 根据半径的tile转换电流最大距离的系数
        private const float tileShockDepthRatio = 4f;

        // 产生瞬间善待你数量
        private const int shockCountMin = 5;
        private const int shockCountMax = 8;
        // 闪电在均匀分布后额外的角度偏差
        private const float shockBias = 140f;
        private const float splitChance = 0.4f;
        // 闪电的参数
        private const float shockEnergyPerRadius = 0.6f / tileSize; // 空气是 0.15；这里每格取 0.6。
        private const float shockAngleBiasRatio = 0.5f; // 每个角度在均匀分布基础上的额外偏差。
        
        private const float edgeShockWaveRadiusMultiplier = 0.25f;


        const int TrailSpritePoolCount = 256;

        private int shockCount;
        private float shockEnergy;
        private float[] shockAngles;
        
        // 按住 Spec 达到此帧数后施放震波。
        private const int ChargeDurationFrames = 40;
        // 提前松开 Spec 后，取消动画持续的帧数。
        private const int CancelDurationFrames = 8;
        // 震波施放后的提示圈淡出持续帧数。
        private const int CastFadeDurationFrames = 12;

        public enum State
        {
            Charge,
            Cancel,
            Cast,
        }
        private int stateAge;
        private State state;
        private int lastStateAge;

        private Player player;
        
        private UnityEngine.Random.State randomState;
        private float radius;
        private int maxPropagationDepth, minDepth;
        ShockWaveHintCircle shockWaveHintCircle;
        Dictionary<ValueTuple<IntVector2, EdgeType>, EdgeShockWave> shockWaveEdges;

        private readonly List<ShockWaveTrailing> homelessTrailings;
        ThunderBoltReactor reactor;
        internal enum EdgeType
        {
            HorizontalSurface,
            VerticalSurface,
            RisingSlope,
            FallingSlope,
            WaterSurface,
            HorizontalBeam,
            VerticalBeam
        }


        public int NextRandomRange(int min, int max)
        {
            var previousState = UnityEngine.Random.state;
            UnityEngine.Random.state = randomState;
            int result = UnityEngine.Random.Range(min, max);
            randomState = UnityEngine.Random.state;
            UnityEngine.Random.state = previousState;
            return result;
        }
        public float NextRandomRange(float min, float max)
        {
            var previousState = UnityEngine.Random.state;
            UnityEngine.Random.state = randomState;
            float result = UnityEngine.Random.Range(min, max);
            randomState = UnityEngine.Random.state;
            UnityEngine.Random.state = previousState;
            return result;
        }
        protected float NextRandomValue()
        {

            var previousState = UnityEngine.Random.state;
            UnityEngine.Random.state = randomState;
            float result = UnityEngine.Random.value;
            randomState = UnityEngine.Random.state;
            UnityEngine.Random.state = previousState;
            return result;
        }
        public static bool IsPlayerReady(Player player, ThunderBoltReactor reactor)
        {
            bool animateFlag = (player.bodyMode == Player.BodyModeIndex.Stand && player.canJump > 0) ||
                    player.bodyMode == Player.BodyModeIndex.Swimming ||
                    player.bodyMode == Player.BodyModeIndex.ZeroG ||
                    (player.bodyMode == Player.BodyModeIndex.Default && player.canJump > 0);   // from Player::watcherDynamicWarpInput
            bool moveFlag = player.input[0].x == 0;
            return player.Consious &&
                !player.Stunned && animateFlag && moveFlag && reactor.reactorEnergy >= ThunderBoltReactor.Config.ShockWaveEnergyRequired;
        }

        private void CastShockWave(Vector2 position)
        {
            IntVector2 tilePosition = room.GetTilePosition(position);
            float shockWaveRadius = radius * edgeShockWaveRadiusMultiplier;
            int tileRadius = Mathf.CeilToInt(shockWaveRadius / tileSize) + 1;
            int spawnCount = 0;
            for (int x = -tileRadius; x <= tileRadius; x++)
            {
                for (int y = -tileRadius; y <= tileRadius; y++)
                {
                    IntVector2 point = tilePosition + new IntVector2(x, y);
                    Vector2 worldPoint = EdgeShockWave.GetPointWorldPosition(point);
                    if (Custom.DistLess(worldPoint, position, shockWaveRadius))
                    {
                        spawnCount += PropagateFromPoint(point, null, maxPropagationDepth);
                    }
                }
            }

            if (room.GetTile(tilePosition).DeepWater)
            {
                Plugin.Log("ShockWave detect deepwater, spawn at surface");
                float waterSurface = room.FloatWaterLevel(position);
                IntVector2 surfaceTile = room.GetTilePosition(new Vector2(position.x, waterSurface));
                spawnCount += PropagateFromPoint(surfaceTile, null, maxPropagationDepth);
            }

            Plugin.Log($"Spawn shockwaveedge with maxdepth {maxPropagationDepth} at center {position} with spawn count {spawnCount}");
        }
        public ShockWaveObject(Player player, float radius, bool showHint, ThunderBoltReactor reactor)
        {
            this.reactor = reactor;
            this.player = player;
            room = player.room;
            shockWaveEdges = new Dictionary<ValueTuple<IntVector2, EdgeType>, EdgeShockWave>();
            randomState = UnityEngine.Random.state;
            this.radius = radius;
            maxPropagationDepth = Mathf.CeilToInt(radius / tileSize * tileShockDepthRatio);
            minDepth = maxPropagationDepth;
            state = State.Charge;
            stateAge = 0;
            shockCount = NextRandomRange(shockCountMin, shockCountMax);
            shockEnergy = radius * shockEnergyPerRadius;
            shockAngles = new float[shockCount];
            InitializeShockAngles();
            if (showHint)
            {
                shockWaveHintCircle = new ShockWaveHintCircle(this);
                room.AddObject(shockWaveHintCircle);
            } else
            {
                shockWaveHintCircle = null;
            }
            homelessTrailings = new List<ShockWaveTrailing>();
        }

        private void InitializeShockAngles()
        {
            float angleStep = 360f / shockCount;
            int maximumBias = Mathf.RoundToInt(shockBias * shockAngleBiasRatio);
            for (int i = 0; i < shockCount; i++)
            {
                // 保留原有随机上限（Random.Range 的整数上限不包含在内）。
                float bias = NextRandomRange(-maximumBias, maximumBias);
                shockAngles[i] = angleStep * i + bias;
            }
        }

        private void ChargeUpdate()
        {
            if (!player.input[0].spec || !IsPlayerReady(player, reactor))
            {
                this.state = State.Cancel;
                this.lastStateAge = this.stateAge;
                this.stateAge = 0;
            }
            else if (this.stateAge >= ChargeDurationFrames)
            {
                this.state = State.Cast;
                this.lastStateAge = this.stateAge;
                this.stateAge = 0;
            }
        }
        private void CancelUpdate()
        {
            if (this.stateAge >= CancelDurationFrames)
            {
                this.Destroy();
                this.shockWaveHintCircle.Destroy();
            }
        }
        private void CastUpdate()
        {
            if (stateAge == 1)
            {
                EmitShockWave();
                reactor.TrySpendEnergy(ThunderBoltReactor.Config.ShockWaveEnergyRequired);
            }

            UpdateEdgeShockWaves();
            if (Expired)
            {
                Plugin.Log("ShockWaveObject destroy");
                Destroy();
                this.shockWaveHintCircle.Destroy();
            }
        }

        bool Expired
        {
            get
            {
                foreach (var e in shockWaveEdges.Values)
                {
                    if (!e.SlatedForDeletion)
                    {
                        return false;
                    }
                }
                foreach (ShockWaveTrailing trailing in homelessTrailings)
            {
                if (!trailing.Expired)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private void EmitShockWave()
        {
            Vector2 position = player.mainBodyChunk.pos;
            CastShockWave(position);
            DamageCreatures(position);

            for (int i = 0; i < shockCount; i++)
            {
                ShockObject shock = new ShockObject(room, position, shockAngles[i], radius, shockEnergy, splitChance);
                room.AddObject(shock);
            }
        }

        private void DamageCreatures(Vector2 position)
        {
            // 以下伤害与硬直数值保持原样，仅整理判定结构。
            foreach (Creature creature in room.updateList.OfType<Creature>().Where(creature => creature != player && creature is not DMPSDrone.DMPSDrone))
            {
                foreach (BodyChunk chunk in creature.bodyChunks)
                {
                    if (Custom.DistLess(chunk.pos, position, chunk.rad + radius))
                    {
                        creature.Violence(player.firstChunk, default, creature.mainBodyChunk, default,
                            Creature.DamageType.Electric, damage, shockStun);
                        break;
                    }
                }
            }
        }

        private void UpdateEdgeShockWaves()
        {
            foreach (EdgeShockWave edge in shockWaveEdges.Values.Where(edge => !edge.SlatedForDeletion).ToList())
            {
                edge.Update();
            }
            foreach (ShockWaveTrailing trailing in homelessTrailings)
            {
                trailing.Age();
            }
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (player.room != this.room)
            {
                this.Destroy();
                return;
            }
            this.stateAge += 1;
            if (this.state == State.Charge)
            {
                ChargeUpdate();   
            }
            else if (state == State.Cancel)
            {
                CancelUpdate();
            }
            else if (state == State.Cast)
            {
                CastUpdate();
            }

        }


        /**********************
         * 边坐标系: 以coord所在tile (i, j, k) 其中k的值:
         *   0
         *  ____
         *  |\ /2
         * 1| X
         *  |/ \3
         * 0: -  1:| 2: / 3:\
         * 
         * 4: 水面  位置视为0
         * 5: -杆子 6: |杆子 类似于0和1
         * 
         * 方向: 0: (优先上然后左), 1: (反之)
         * 
         * 点坐标系: (i, j)表示(i, j)左上角的点
         * 
         * 存储方式是通过边坐标系
         * 点坐标系仅用来帮助判断联通
         * 
         * ******************/
        public bool HasShockWaveEdge(ValueTuple<IntVector2, EdgeType> edgePosition)
        {
            if (shockWaveEdges.ContainsKey(edgePosition) && shockWaveEdges[edgePosition].SlatedForDeletion)
            {
                shockWaveEdges.Remove(edgePosition);
            }

            return shockWaveEdges.ContainsKey(edgePosition);
        }

        private void SpawnShockWaveEdge(ValueTuple<IntVector2, EdgeType> edgePosition, int direction, int remainingDepth, ShockWaveTrailing trail)
        {
            if (shockWaveEdges.ContainsKey(edgePosition))
            {
                return;
            }

            EdgeShockWave edgeShockWave = new EdgeShockWave(this, edgePosition, direction, remainingDepth, trail);
            minDepth = Mathf.Min(minDepth, remainingDepth);
            shockWaveEdges.Add(edgePosition, edgeShockWave);
        }

        private int PropagateFromPoint(IntVector2 point, EdgeShockWave sourceEdge, int remainingDepth)
        {
            int spawnCount = 0;
            ShockWaveTrailing trail = null;
            if (sourceEdge is not null)
                trail = sourceEdge.GetTrail();
            foreach (var connectedEdge in EdgeShockWave.pointConnectedEdges)
            {
                var (edgeOffset, direction) = connectedEdge;
                IntVector2 edgeTile = point + edgeOffset.Item1;
                var edge = (edgeTile, edgeOffset.Item2);
                if (edge == sourceEdge?.EdgePosition || HasShockWaveEdge(edge))
                {
                    continue;
                }

                if (EdgeShockWave.IsValidEdge(room, edge))
                {
                    if (trail is null)
                    {
                        trail = new ShockWaveTrailing(this, EdgeShockWave.GetPointWorldPosition(point));
                    }
                    SpawnShockWaveEdge(edge, direction, remainingDepth, trail);
                    trail = null;
                    spawnCount++;
                }
            }
            if (trail is not null)
            {
                homelessTrailings.Add(trail);
            }
            return spawnCount;
        }

        // 根据剩余传播深度计算强度，影响火花速度、生成概率以及传播时间。
        internal float GetPropagationStrength()
        {
            return Mathf.Pow(Mathf.InverseLerp(0, maxPropagationDepth, minDepth), 2);
        }
        private sealed class EdgeShockWave
        {
            // 对于边类型以及来源方向，得到目标点相对于边坐标的偏移。
            private static readonly IntVector2[][] targetPoints =
            [
                [new IntVector2(1, 0), new IntVector2(0, 0)],
                [new IntVector2(0, -1), new IntVector2(0, 0)],
                [new IntVector2(0, -1), new IntVector2(1, 0)],
                [new IntVector2(1, -1), new IntVector2(0, 0)],
                [new IntVector2(1, 0), new IntVector2(0, 0)],
                [new IntVector2(1, 0), new IntVector2(0, 0)],
                [new IntVector2(0, -1), new IntVector2(0, 0)]
            ];

            // 点所连接的边：（坐标偏移、边类型）及从该点进入边的方向。
            public static readonly ValueTuple<ValueTuple<IntVector2, EdgeType>, int>[] pointConnectedEdges =
            [
                ((new IntVector2(0, 0), EdgeType.HorizontalSurface), 0), ((new IntVector2(-1, 0), EdgeType.HorizontalSurface), 1),
                ((new IntVector2(0, 0), EdgeType.VerticalSurface), 0), ((new IntVector2(0, 1), EdgeType.VerticalSurface), 1),
                ((new IntVector2(-1, 0), EdgeType.RisingSlope), 0), ((new IntVector2(0, 1), EdgeType.RisingSlope), 1),
                ((new IntVector2(0, 0), EdgeType.FallingSlope), 0), ((new IntVector2(-1, 1), EdgeType.FallingSlope), 1),
                ((new IntVector2(0, 0), EdgeType.WaterSurface), 0), ((new IntVector2(-1, 0), EdgeType.WaterSurface), 1),
                ((new IntVector2(0, 0), EdgeType.HorizontalBeam), 0), ((new IntVector2(-1, 0), EdgeType.HorizontalBeam), 1),
                ((new IntVector2(0, 0), EdgeType.VerticalBeam), 0), ((new IntVector2(0, 1), EdgeType.VerticalBeam), 1)
            ];

            const int minimumTravelTime = 1, maximumTravelTime = 3;   // 沿一条水平边传播的时间范围；斜边会 ceil(travelTime * 1.414)。
            const int lingerTime = 10;
            const float minimumTravelTimeMultiplier = 0.25f, minimumEffectStrength = 0.2f;
            const float mouseSparkSpawnChance = 0.5f, sparkSpawnChance = 0.9f, neuronSparkSpawnFactor = 1.5f;
            const float sparkAngleSpread = 43f, sparkSpeed = 6f, mouseSparkSpeed = 3f;
            const float mouseSparkLife = 18f;
            const float zapFlashSize = 0.3f, sparkFlashIntensity = 4f;
            const int sparkStandardLife = 12, sparkExceptionalLife = 18;
            readonly Color sparkColor = LaserDroneGraphics.defaultLaserColor;
            
            // 对于附近其他生物的击晕范围以及时长
            const int stunTime = 30;
            const float stunRadius = 10f;

            public static Vector2 GetPointWorldPosition(IntVector2 point)
            {
                return new Vector2(point.x * tileSize, tileSize + point.y * tileSize);
            }

            public static Vector2 GetEdgeWorldPosition(Room room, ValueTuple<IntVector2, EdgeType> edgePosition, int direction, float interpolation)
            {
                IntVector2 point0 = edgePosition.Item1 + targetPoints[(int)edgePosition.Item2][1 - direction];
                IntVector2 point1 = edgePosition.Item1 + targetPoints[(int)edgePosition.Item2][direction];
                Vector2 position = Vector2.Lerp(GetPointWorldPosition(point0), GetPointWorldPosition(point1), interpolation);
                if (edgePosition.Item2 == EdgeType.WaterSurface)
                {
                    float waterY = room.FloatWaterLevel(position);
                    position.y = Mathf.Clamp(waterY, edgePosition.Item1.y * tileSize, edgePosition.Item1.y * tileSize + tileSize);
                }

                if (edgePosition.Item2 == EdgeType.HorizontalBeam)
                {
                    position.y -= tileSize / 2f;
                }
                else if (edgePosition.Item2 == EdgeType.VerticalBeam)
                {
                    position.x += tileSize / 2f;
                }

                return position;
            }

            // 得到边发射电火花的角度（默认传入合法边）。
            public float GetSparkEmissionAngle(ValueTuple<IntVector2, EdgeType> edgePosition)
            {
                Room.Tile tile = parent.room.GetTile(edgePosition.Item1);
                switch (edgePosition.Item2)
                {
                    case EdgeType.HorizontalSurface:
                        return tile.Solid ? 0f : 180f;
                    case EdgeType.VerticalSurface:
                        return tile.Solid ? 270f : 90f;
                    case EdgeType.RisingSlope:
                        return parent.room.GetTile(edgePosition.Item1 + new IntVector2(0, 1)).Solid ? 135f : -45f;
                    case EdgeType.FallingSlope:
                        return parent.room.GetTile(edgePosition.Item1 + new IntVector2(0, 1)).Solid ? 225f : 45f;
                    case EdgeType.WaterSurface:
                        return 0f;
                    case EdgeType.HorizontalBeam:
                        return parent.NextRandomValue() < 0.5f ? 0f : 180f;
                    case EdgeType.VerticalBeam:
                        return parent.NextRandomValue() < 0.5f ? 90f : 270f;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(edgePosition), edgePosition.Item2, "无效的边类型");
                }
            }

            public static bool IsValidEdge(Room room, ValueTuple<IntVector2, EdgeType> edgePosition)
            {
                var (tilePosition, edgeType) = edgePosition;
                switch (edgeType)
                {
                    case EdgeType.RisingSlope:
                    case EdgeType.FallingSlope:
                        return IsValidSlopeEdge(room, tilePosition, edgeType);
                    case EdgeType.HorizontalSurface:
                        if (room.GetTile(tilePosition).Terrain == Room.Tile.TerrainType.Floor)
                        {
                            return true;
                        }
                        if (room.GetTile(tilePosition).Terrain == Room.Tile.TerrainType.Slope || room.GetTile(tilePosition + new IntVector2(0, 1)).Terrain == Room.Tile.TerrainType.Slope)
                            return false;
                        return room.GetTile(tilePosition).IsSolid() ^ room.GetTile(tilePosition + new IntVector2(0, 1)).IsSolid();
                    case EdgeType.VerticalSurface:
                        if (room.GetTile(tilePosition).Terrain == Room.Tile.TerrainType.Slope || room.GetTile(tilePosition + new IntVector2(-1, 0)).Terrain == Room.Tile.TerrainType.Slope)
                            return false;
                        return room.GetTile(tilePosition).IsSolid() ^ room.GetTile(tilePosition + new IntVector2(-1, 0)).IsSolid();
                    case EdgeType.WaterSurface:
                        if (!room.water)
                        {
                            return false;
                        }

                        float waterY = room.FloatWaterLevel(room.MiddleOfTile(tilePosition));
                        return tilePosition.y * tileSize <= waterY && waterY <= tilePosition.y * tileSize + tileSize;
                    case EdgeType.HorizontalBeam:
                        return room.GetTile(tilePosition).horizontalBeam;
                    case EdgeType.VerticalBeam:
                        return room.GetTile(tilePosition).verticalBeam;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(edgePosition), edgeType, "无效的边类型");
                }
            }

            private static bool IsValidSlopeEdge(Room room, IntVector2 tilePosition, EdgeType edgeType)
            {
                if (tilePosition.x < 0 || tilePosition.x >= room.TileWidth - 1 ||
                    tilePosition.y < 0 || tilePosition.y >= room.TileHeight - 1)
                {
                    return false;
                }

                if (room.GetTile(tilePosition).Terrain != Room.Tile.TerrainType.Slope)
                {
                    return false;
                }

                bool[] isSolid =
                [
                    room.GetTile(tilePosition + Custom.fourDirections[0]).IsSolid(),
                    room.GetTile(tilePosition + Custom.fourDirections[1]).IsSolid(),
                    room.GetTile(tilePosition + Custom.fourDirections[2]).IsSolid(),
                    room.GetTile(tilePosition + Custom.fourDirections[3]).IsSolid()
                ]; // 左、下、右、上。

                if (edgeType == EdgeType.RisingSlope)
                {
                    return (isSolid[1] && isSolid[2]) || (isSolid[3] && isSolid[0]);
                }

                return (isSolid[0] && isSolid[1]) || (isSolid[2] && isSolid[3]);
            }

            private ShockWaveObject parent;
            private int remainingDepth;
            public ValueTuple<IntVector2, EdgeType> EdgePosition
            {
                get;
                private set;
            }
            public int Direction;
            public int TravelTime;
            public int Age;
            private float effectStrength;
            public bool SlatedForDeletion;
            private ShockWaveTrailing Trail { get; }
            internal ShockWaveTrailing GetTrail() => Trail;
            internal void DrawTrail(ShockWaveObject owner, FSprite[] sprites, ref int spriteIndex, int endIndex, Vector2 cameraPosition)
                => Trail.Draw(owner, sprites, ref spriteIndex, endIndex, cameraPosition);

            public EdgeShockWave(
                ShockWaveObject parent, ValueTuple<IntVector2, EdgeType> edgePosition, int direction, int remainingDepth, ShockWaveTrailing trail
                )
            {
                EdgePosition = edgePosition;
                this.parent = parent;
                this.remainingDepth = remainingDepth;
                Direction = direction;
                Trail = trail;
                Age = 0;
                TravelTime = parent.NextRandomRange(minimumTravelTime, maximumTravelTime);
                if (edgePosition.Item2 == EdgeType.RisingSlope || edgePosition.Item2 == EdgeType.FallingSlope)
                {
                    TravelTime = Mathf.CeilToInt(TravelTime * diagonalLength);
                }

                TravelTime = Mathf.CeilToInt(TravelTime * Mathf.Lerp(minimumTravelTimeMultiplier, 1f, parent.GetPropagationStrength()));
                effectStrength = Mathf.Lerp(minimumEffectStrength, 1f, parent.GetPropagationStrength());
            }
            public bool Expired => Age > TravelTime;
            public void Update()
            {
                if (Age >= TravelTime + lingerTime)
                {
                    SlatedForDeletion = true;
                    return;
                }

                if (Age <= TravelTime)
                {
                    Vector2 worldPosition = GetEdgeWorldPosition(parent.room, EdgePosition, Direction, Age * 1.0f / TravelTime);
                    Trail.Age();
                    Trail.UpdatePosition(parent, worldPosition);
                    float emissionAngle = GetSparkEmissionAngle(EdgePosition);
                    if (parent.NextRandomValue() < mouseSparkSpawnChance * effectStrength)
                    {
                        float angle = emissionAngle + parent.NextRandomRange(-Mathf.RoundToInt(sparkAngleSpread), Mathf.RoundToInt(sparkAngleSpread));
                        var spark = new MouseSpark(worldPosition, Custom.DegToVec(angle) * mouseSparkSpeed * effectStrength, mouseSparkLife, sparkColor);
                        parent.room.AddObject(spark);
                    }
                    if (parent.NextRandomValue() < sparkSpawnChance * effectStrength)
                    {
                        float angle = emissionAngle + parent.NextRandomRange(-Mathf.RoundToInt(sparkAngleSpread), Mathf.RoundToInt(sparkAngleSpread));
                        var spark = new Spark(worldPosition, Custom.DegToVec(angle) * sparkSpeed * effectStrength, sparkColor, null, sparkStandardLife, sparkExceptionalLife);
                        parent.room.AddObject(spark);
                    }
                    
                    if (parent.NextRandomValue() < neuronSparkSpawnFactor * effectStrength)
                    {
                        var neuronSpark = new NeuronSpark(worldPosition);
                        parent.room.AddObject(neuronSpark);
                    }
                    var creatures = from creature in parent.room.updateList
                                    where creature is Creature
                                    where creature != parent.player && creature is not DMPSDrone.DMPSDrone
                                    select creature as Creature;
                    bool spawnFlash = false;
                    foreach (Creature creature in creatures.ToList())
                    {
                        foreach (BodyChunk chunk in creature.bodyChunks)
                        {
                            if (Custom.DistLess(chunk.pos, worldPosition, stunRadius + chunk.rad))
                            {
                                creature.Stun(stunTime);
                                if (!spawnFlash)
                                {
                                    spawnFlash = true;
                                    var spark = new ElectricDeath.SparkFlash(worldPosition, sparkFlashIntensity);
                                    parent.room.AddObject(spark);
                                }
                                break;
                            }
                        }
                    }
                    if (Age == TravelTime)
                    {
                        if (remainingDepth > 0)
                        {
                            IntVector2 targetPoint = EdgePosition.Item1 + targetPoints[(int)EdgePosition.Item2][Direction];
                            parent.PropagateFromPoint(targetPoint, this, remainingDepth - 1);
                        }
                    }
                }
                Age += 1;
            }

        }
        private sealed class ShockWaveTrailing
        {
            private const int UpdatesPerUpdate = 2;
            private const int GenerationLifetime = 40;
            private const float AlphaDecayPerGeneration = 0.025f;
            private const float VerticalSparkDistance = 10f;
            private const float HorizontalSparkDistance = 2f;
            private const float ElectricWidth = 2.5f;
            private const float PositionJitter = 2f;

            private readonly Queue<TrailingPixel> trailingPixels = new();
            private readonly List<Vector2> positionHistory;
            private readonly ShockWaveObject parent;
            private float lastAngle;
            private int generation;

            private readonly struct TrailingPixel
            {
                public TrailingPixel(Vector2 position, int generation)
                {
                    Position = position;
                    Generation = generation;
                }

                public Vector2 Position { get; }
                public int Generation { get; }
            }

            public ShockWaveTrailing(ShockWaveObject parent, Vector2 startPosition)
            {
                this.parent = parent;
                trailingPixels.Enqueue(new TrailingPixel(startPosition, 0));
                positionHistory = new List<Vector2> { startPosition };
            }

            public bool Expired => trailingPixels.Count == 0 ||
                (trailingPixels.Count == 1 && trailingPixels.Peek().Generation < generation - GenerationLifetime);

            public void UpdatePosition(ShockWaveObject randomSource, Vector2 nextPosition)
            {
                Vector2 previousPosition = positionHistory.Last();
                float directionAngle = Custom.AimFromOneVectorToAnother(previousPosition, nextPosition);
                if (lastAngle == 0f)
                {
                    lastAngle = directionAngle;
                }

                Vector2 direction = (nextPosition - previousPosition).normalized;
                for (int i = 0; i < UpdatesPerUpdate; i++)
                {
                    Vector2 basePosition = Vector2.Lerp(previousPosition, nextPosition, (float)(i + 1) / UpdatesPerUpdate);
                    lastAngle = Mathf.LerpAngle(lastAngle, directionAngle, 0.2f);
                    Vector2 perpendicular = Custom.DegToVec(lastAngle + ((generation * UpdatesPerUpdate + i) % 2 == 0 ? 90f : -90f));
                    float distance = randomSource.NextRandomRange(VerticalSparkDistance / 2f, VerticalSparkDistance) * parent.GetPropagationStrength();
                    float horizontalDistance = randomSource.NextRandomRange(-HorizontalSparkDistance, HorizontalSparkDistance);
                    trailingPixels.Enqueue(new TrailingPixel(basePosition + perpendicular * distance + direction * horizontalDistance, generation));
                }

                positionHistory.Add(nextPosition);
            }

            public void Age()
            {
                generation++;
                while (trailingPixels.Count > 1 && trailingPixels.Peek().Generation < generation - GenerationLifetime)
                {
                    trailingPixels.Dequeue();
                }
            }

            public void Draw(ShockWaveObject randomSource, FSprite[] sprites, ref int spriteIndex, int endIndex, Vector2 cameraPosition)
            {
                if (Expired)
                {
                    return;
                }

                Vector2 previousPosition = default;
                bool isFirst = true;
                foreach (TrailingPixel pixel in trailingPixels)
                {
                    if (spriteIndex >= endIndex)
                    {
                        break;
                    }

                    Vector2 position = pixel.Position + new Vector2(
                        randomSource.NextRandomRange(-PositionJitter, PositionJitter),
                        randomSource.NextRandomRange(-PositionJitter, PositionJitter));
                    if (!isFirst)
                    {
                        FSprite sprite = sprites[spriteIndex];
                        sprite.scaleX = ElectricWidth;
                        sprite.scaleY = Vector2.Distance(position, previousPosition);
                        sprite.anchorY = 0f;
                        sprite.SetPosition(previousPosition - cameraPosition);
                        sprite.rotation = Custom.AimFromOneVectorToAnother(previousPosition, position);
                        sprite.isVisible = true;
                        sprite.alpha = parent.GetPropagationStrength() * GetAlpha(pixel.Generation);
                        spriteIndex++;
                    }

                    previousPosition = position;
                    isFirst = false;
                }
            }

            private float GetAlpha(int pixelGeneration)
            {
                return Mathf.Clamp01(1f - (generation - pixelGeneration) * AlphaDecayPerGeneration);
            }
        }

        private sealed class ShockWaveHintCircle : UpdatableAndDeletable, IDrawable
        {
            private const float ColorIndex = 1f / 255f;
            private const float OuterAlpha = 0.05f;
            private const float InnerAlpha = 0.15f;
            private const float CancelMaxScale = 10f;
            private const float SpriteTextureRadius = 8f;
            private const string TargetContainer = "Foreground";

            private readonly ShockWaveObject owner;
            public ShockWaveHintCircle(ShockWaveObject owner)
            {
                this.owner = owner;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[3];
                sLeaser.sprites[0] = new FSprite("Futile_White", true);
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["VectorCircleFadable"];
                sLeaser.sprites[1] = new FSprite("Futile_White", true);
                sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["VectorCircleFadable"];
                sLeaser.sprites[2] = new FSprite("Futile_White", true);
                sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["VectorCircleFadable"];
                AddToContainer(sLeaser, rCam);
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                float innerRad;
                Vector2 playerPos = Vector2.Lerp(owner.player.mainBodyChunk.lastPos, owner.player.mainBodyChunk.pos, timeStacker);
                sLeaser.sprites[0].SetPosition(playerPos - camPos);
                sLeaser.sprites[1].SetPosition(playerPos - camPos);
                sLeaser.sprites[2].SetPosition(playerPos - camPos);
                if (owner.state == State.Charge)
                {
                    innerRad = Mathf.Clamp01(owner.stateAge * 1.0f / ShockWaveObject.ChargeDurationFrames);
                    innerRad *= owner.radius;
                    sLeaser.sprites[1].scale = innerRad / SpriteTextureRadius;
                    sLeaser.sprites[0].scale = owner.radius / SpriteTextureRadius;
                    sLeaser.sprites[2].scale = owner.radius * ShockWaveObject.edgeShockWaveRadiusMultiplier / SpriteTextureRadius;
                    sLeaser.sprites[0].color = new Color(ColorIndex, 0, OuterAlpha);
                    sLeaser.sprites[1].color = new Color(ColorIndex, 0, InnerAlpha);
                    sLeaser.sprites[2].color = new Color(0, 0, 1, 0.05f);
                }
                else if (owner.state == State.Cast)
                {
                    float decay = Mathf.Lerp(1f, 0f, owner.stateAge * 1.0f / ShockWaveObject.CastFadeDurationFrames);
                    decay = Mathf.Clamp01(decay);
                    float alphaDecay = decay;
                    float scaleDecay = DMHelper.EaseInOutCubic(decay);
                    sLeaser.sprites[0].scale = owner.radius / SpriteTextureRadius * scaleDecay;
                    sLeaser.sprites[1].scale = owner.radius / SpriteTextureRadius * scaleDecay;
                    sLeaser.sprites[2].scale = owner.radius * ShockWaveObject.edgeShockWaveRadiusMultiplier / SpriteTextureRadius * scaleDecay;
                    sLeaser.sprites[0].color = new Color(ColorIndex, 0, OuterAlpha * alphaDecay);
                    sLeaser.sprites[1].color = new Color(ColorIndex, 0, InnerAlpha * alphaDecay);
                    sLeaser.sprites[2].color = new Color(0, 0, 1 * alphaDecay, 0.05f);
                }
                else if (owner.state == State.Cancel)
                {
                    innerRad = Mathf.Clamp01(owner.lastStateAge * 1.0f / ShockWaveObject.ChargeDurationFrames);
                    innerRad *= owner.radius;
                    float decay = Mathf.Lerp(1f, 0f, owner.stateAge * 1.0f / ShockWaveObject.CancelDurationFrames);
                    decay = Mathf.Clamp01(decay);
                    float scaleMultiplier = Mathf.Lerp(1f, CancelMaxScale, 1f - decay);
                    float alphaDecay = decay;
                    sLeaser.sprites[0].scale = owner.radius / SpriteTextureRadius * scaleMultiplier;
                    sLeaser.sprites[1].scale = innerRad / SpriteTextureRadius * scaleMultiplier;
                    sLeaser.sprites[2].scale = owner.radius * ShockWaveObject.edgeShockWaveRadiusMultiplier / SpriteTextureRadius * scaleMultiplier;
                    sLeaser.sprites[0].color = new Color(ColorIndex, 0, OuterAlpha * alphaDecay);
                    sLeaser.sprites[1].color = new Color(ColorIndex, 0, InnerAlpha * alphaDecay);
                    sLeaser.sprites[2].color = new Color(0, 0, 1 * alphaDecay, 0.05f);
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer = null)
            {
                if (newContainer == null)
                {
                    newContainer = rCam.ReturnFContainer(TargetContainer);
                }

                foreach (FSprite sprite in sLeaser.sprites)
                {
                    sprite.RemoveFromContainer();
                }

                foreach (FSprite sprite in sLeaser.sprites)
                {
                    newContainer.AddChild(sprite);
                }
            }
        }
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TrailSpritePoolCount];
            for (int i = 0; i < TrailSpritePoolCount; i++)
            {
                sLeaser.sprites[i] = new FSprite("pixel");
                sLeaser.sprites[i].scale = 2f;
                sLeaser.sprites[i].color = LaserDroneGraphics.defaultLaserColor;
            }
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
            }
            int spriteIdx = 0, maxIdx = spriteIdx + TrailSpritePoolCount;
            foreach (var edge in shockWaveEdges.Values)
            {
                if (edge.SlatedForDeletion || edge.Expired || edge.GetTrail() is null)
                    continue;
                edge.DrawTrail(this, sLeaser.sprites, ref spriteIdx, maxIdx, camPos);
            }
            foreach (var trail in homelessTrailings)
            {
                if (trail.Expired)
                    continue;
                trail.Draw(this, sLeaser.sprites, ref spriteIdx, maxIdx, camPos);
            }
            for (int i = spriteIdx; i < maxIdx; i++)
            {
                sLeaser.sprites[i].isVisible = false;
            }
            Plugin.Log($"use {spriteIdx} / {maxIdx} pixel obj.");
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer = null)
        {
            if (newContainer == null)
            {
                newContainer = rCam.ReturnFContainer("Foreground");
            }
            for (int i = 0; i < sLeaser.sprites.Length; ++i)
            {
                sLeaser.sprites[i].RemoveFromContainer();
            }
            for (int i = 0; i < sLeaser.sprites.Length; ++i)
            {
                newContainer.AddChild(sLeaser.sprites[i]);
            }
        }
    }
}
