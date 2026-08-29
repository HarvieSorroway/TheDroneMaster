using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using DMPSDroneCreature = TheDroneMaster.DMPS.DMPSDrone.DMPSDrone;
using LegacyDroneCreature = TheDroneMaster.LaserDrone;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSutils
{
    internal class ShockObject : UpdatableAndDeletable, IDrawable
    {
        // 逻辑层只在固定间距上积分介质耗能；视觉折线不会参与命中或伤害计算。
        const float airCost = 0.15f, waterCost = 0f, solidCost = 1.5f, beamCost = 0f, slopeCost = 0.65f;
        const float minEnergy = 0.05f;
        const float logicSampleSpacing = 15f, terrainProbeSpacing = 8f, routeLookAhead = 140f, wallTurnBackoff = 12f;
        const float wallBodyHitTolerance = 24f, beamCaptureRadius = 6f, beamPathCostPerTile = 0.08f;
        const float firstDecisionMinDistance = 65f, firstDecisionMaxDistance = 100f;
        const float leaderDecisionMinDistance = 45f, leaderDecisionMaxDistance = 85f;
        const float localTurnCostPerDegree = 0.003f, mainDeviationCostPerDegree = 0.0015f;
        const float leaderNoise = 0.16f, bodyAttractionRange = 320f, bodyAttractionStrength = 0.22f;
        const float branchEnergyShare = 0.42f, minBranchEnergy = 0.65f, bodyCollisionRadius = 2f;
        const float visualMinSegmentLength = 24f, visualOffsetFactor = 0.28f, visualMaxOffset = 40f;
        const float visualAmplitudeDecay = 0.62f, visualMinOffsetFactor = 0.2f;
        const float visualInitialStraightLength = 18f;
        const int maxLife = 40, initialSplitBudget = 3;
        const int maxRegularBranches = 3, maxBeamBranches = 2, maxTotalBranches = 4;
        const int maxLogicalSegments = 24, maxDecisionsPerPath = 8, maxVisualDepth = 4;

        // 有限候选角能把路径搜索约束为常数规模，避免逐像素随机步进带来的大量碰撞查询。
        static readonly float[] routeAngleOffsets = new float[] { 0f, -12f, 12f, -24f, 24f, -40f, 40f, -60f, 60f, -80f, 80f, -110f, 110f };


        Vector2 pos;
        float shootAngle, biasRange, initEnergy, splitChance, energyCostScale;
        PhysicalObject source;
        int _seed = -1;

        // 逻辑路径与命中状态
        Random.State _state;
        readonly List<LogicSegment> logicSegments = new List<LogicSegment>(maxLogicalSegments);
        readonly HashSet<BodyChunk> hitChunks = new HashSet<BodyChunk>();
        readonly List<BodyChunk> attractableChunks = new List<BodyChunk>();
        readonly HashSet<int> visitedBeamTiles = new HashSet<int>();
        bool generated;
        int generatedRegularBranches, generatedBeamBranches;

        internal IReadOnlyList<LogicSegment> LogicSegments => logicSegments;
        internal IReadOnlyCollection<BodyChunk> HitChunks => hitChunks;

        // 由逻辑段细分出的纯视觉连接
        FContainer _container;
        readonly List<ShockConnection> shocks = new List<ShockConnection>(maxLogicalSegments * 4);

        int life = 0, lastLife = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShockObject"/> class.
        /// </summary>
        /// <param name="room"></param>
        /// <param name="pos">闪电的发射位置</param>
        /// <param name="shootAngle">主链的初始发射角度</param>
        /// <param name="biasRange">闪电分裂的角度</param>
        /// <param name="initEnergy"></param>
        /// <param name="splitChance">提前分裂的概率，同时也用于控制闪电是偏向单主链还是分支链</param>
        /// <param name="falloff">闪电衰减，越接近0衰减越大</param>
        public ShockObject(Room room, Vector2 pos, float shootAngle, float biasRange, float initEnergy, float splitChance, float falloff = 1f, int seed = -1, PhysicalObject source = null)
        {
            this.room = room;
            this.pos = pos;
            this.shootAngle = shootAngle;
            this.biasRange = biasRange;
            this.initEnergy = initEnergy;
            this.splitChance = splitChance;
            this.source = source;
            energyCostScale = 1f / Mathf.Clamp(falloff, 0.1f, 1f);
            _seed = seed >= 0 ? seed : Random.Range(0, 114514);

            Init();
        }

        void Init()
        {
            // ShockObject 使用自己的随机状态，生成过程不会扰乱 Unity 全局随机序列。
            var state = Random.state;
            Random.InitState(_seed);
            _state = Random.state;
            Random.state = state;
        }

        void GenerateLightning()
        {
            var state = Random.state;
            Random.state = _state;

            CollectAttractableChunks();

            // 主链和支链共用队列及全局段数预算，防止分裂数量失控。
            Queue<PathState> pendingBranches = new Queue<PathState>();
            pendingBranches.Enqueue(new PathState(pos, shootAngle, initEnergy, initialSplitBudget, true, 0));

            while (pendingBranches.Count > 0 && logicSegments.Count < maxLogicalSegments)
            {
                PathState path = pendingBranches.Dequeue();
                GrowPath(ref path, pendingBranches);
            }

            _state = Random.state;
            Random.state = state;
            generated = true;
        }

        void CollectAttractableChunks()
        {
            // 玩家、无人机和发射源不会吸引或截获闪电，其余可受武器影响的 BodyChunk 才参与计算。
            attractableChunks.Clear();
            for (int layer = 0; layer < room.physicalObjects.Length; layer++)
            {
                foreach (PhysicalObject obj in room.physicalObjects[layer])
                {
                    if (IsIgnoredElectricalObject(obj))
                        continue;

                    for (int i = 0; i < obj.bodyChunks.Length; i++)
                        attractableChunks.Add(obj.bodyChunks[i]);
                }
            }
        }

        void GrowPath(ref PathState path, Queue<PathState> pendingBranches)
        {
            // 每轮只生成“当前位置到下一个特殊判断点”的逻辑段，而不是沿途逐点改变方向。
            while (path.energy > minEnergy && path.decisions < maxDecisionsPerPath && logicSegments.Count < maxLogicalSegments)
            {
                TravelResult travel = TraceToDecision(path);
                if (Vector2.Distance(path.pos, travel.pos) > 0.5f)
                {
                    LogicSegment segment = new LogicSegment(path.pos, travel.pos, path.energy, travel.energy, travel.hitChunk, path.branchDepth);
                    logicSegments.Add(segment);
                    AddVisualSegment(segment);
                }

                path.pos = travel.pos;
                path.energy = travel.energy;
                path.firstSegment = false;

                if (travel.hitChunk != null)
                    hitChunks.Add(travel.hitChunk);

                if (path.energy <= minEnergy || travel.reason == DecisionReason.EnergyDepleted)
                    break;

                if (travel.reason == DecisionReason.Beam)
                {
                    // 接触 Beam 后由 Beam 的连通方向接管传播，原来的自由空间主链在这里结束。
                    GrowAlongBeam(path, pendingBranches);
                    break;
                }

                float targetDirection = path.direction;
                bool hasTarget = travel.reason == DecisionReason.BodyChunk
                    && TryGetPreferredTargetDirection(path.pos, path.direction, out targetDirection);
                bool mustTurn = travel.reason == DecisionReason.Obstacle
                    || travel.reason == DecisionReason.BodyChunk;

                float nextDirection;
                float turnCost;
                if (travel.reason == DecisionReason.BodyChunk && hasTarget)
                {
                    // 命中后立即转向一个尚未命中的可见 BodyChunk，形成明确的链式电击。
                    nextDirection = targetDirection;
                    turnCost = GetTurnEnergyCost(path.direction, nextDirection);
                    room.AddObject(new ShockSpasm(room, source, travel.hitChunk, (int)Mathf.Lerp(5, 40, Mathf.InverseLerp(0,3,travel.energy)), (travel.hitChunk.rad + 20f) * 2f / 80f, travel.energy));
                }
                else if (!TryChooseLowestLossDirection(path.pos, path.direction, mustTurn, hasTarget, targetDirection, float.NaN, out nextDirection, out turnCost))
                {
                    break;
                }

                if (turnCost >= path.energy)
                    break;

                TryCreateBranch(ref path, nextDirection, hasTarget, targetDirection, pendingBranches);

                if (turnCost >= path.energy)
                    break;

                path.energy -= turnCost;
                path.direction = nextDirection;
                path.decisions++;
            }
        }

        TravelResult TraceToDecision(PathState path)
        {
            // 判断点只可能是随机领头点、墙体、BodyChunk、Beam 或能量耗尽位置。
            float energyDistance = Mathf.Min(1200f, path.energy / Mathf.Max(0.001f, airCost * energyCostScale) * logicSampleSpacing);
            float decisionDistance = path.firstSegment
                ? Random.Range(firstDecisionMinDistance, firstDecisionMaxDistance)
                : Random.Range(leaderDecisionMinDistance, leaderDecisionMaxDistance);
            float requestedDistance = Mathf.Min(energyDistance, decisionDistance);

            Vector2 dir = Custom.DegToVec(path.direction);
            Vector2 plannedEnd = path.pos + dir * requestedDistance;
            Vector2 terrainEnd = TraceToTerrain(path.pos, dir, requestedDistance, out bool hitObstacle);
            BodyChunk hitChunk = FindFirstBodyChunk(path.pos, plannedEnd, out Vector2 hitPos);
            float terrainDistance = Vector2.Distance(path.pos, terrainEnd);
            float bodyDistance = hitChunk == null ? float.MaxValue : Vector2.Distance(path.pos, hitPos);
            // 给贴墙 BodyChunk 留出少量容差，避免墙体采样误差让闪电在即将命中时绕开目标。
            bool bodyWins = hitChunk != null && (!hitObstacle || bodyDistance <= terrainDistance + wallBodyHitTolerance);

            // 同一条前进射线上，实际距离最近的 BodyChunk / Beam 优先成为判断点。
            bool foundBeam = FindFirstBeamPoint(path.pos, terrainEnd, out Vector2 beamPoint);
            float beamDistance = foundBeam ? Vector2.Distance(path.pos, beamPoint) : float.MaxValue;
            if (bodyWins && bodyDistance <= beamDistance)
            {
                foundBeam = false;
            }
            else
            {
                hitChunk = null;
            }

            Vector2 end = hitChunk != null ? hitPos : foundBeam ? beamPoint : terrainEnd;
            DecisionReason reason = hitChunk != null
                ? DecisionReason.BodyChunk
                : foundBeam
                    ? DecisionReason.Beam
                    : hitObstacle
                        ? DecisionReason.Obstacle
                        : requestedDistance < energyDistance
                            ? DecisionReason.LeaderPoint
                            : DecisionReason.EnergyDepleted;

            bool wallAdjacentBodyHit = hitChunk != null && hitObstacle && bodyDistance > terrainDistance;
            float remainingEnergy;
            bool depleted;
            if (wallAdjacentBodyHit)
            {
                // 容差区内的贴墙命中只按安全路径和最后一小段接触距离扣能量，不允许穿墙继续传播。
                Vector2 safeEnd = terrainEnd;
                remainingEnergy = ConsumeTravelEnergy(path.pos, ref safeEnd, path.energy, out depleted);
                if (!depleted)
                {
                    float contactCost = (bodyDistance - terrainDistance) / logicSampleSpacing * airCost * energyCostScale;
                    remainingEnergy = Mathf.Max(0f, remainingEnergy - contactCost);
                    end = hitPos;
                }
                else
                {
                    end = safeEnd;
                }
            }
            else
            {
                remainingEnergy = ConsumeTravelEnergy(path.pos, ref end, path.energy, out depleted);
            }

            if (depleted)
            {
                hitChunk = null;
                reason = DecisionReason.EnergyDepleted;
            }

            return new TravelResult(end, remainingEnergy, reason, hitChunk);
        }

        Vector2 TraceToTerrain(Vector2 from, Vector2 dir, float maxDistance, out bool hitObstacle)
        {
            hitObstacle = false;
            Vector2 lastSafe = from;

            for (float distance = terrainProbeSpacing; distance <= maxDistance + terrainProbeSpacing; distance += terrainProbeSpacing)
            {
                float clampedDistance = Mathf.Min(distance, maxDistance);
                Vector2 sample = from + dir * clampedDistance;
                if (room.GetTile(sample).Solid)
                {
                    hitObstacle = true;
                    Vector2 solid = sample;
                    // 在最后安全点与固体采样点间做二分，减少较大探测步长造成的墙面误差。
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 middle = Vector2.Lerp(lastSafe, solid, 0.5f);
                        if (room.GetTile(middle).Solid)
                            solid = middle;
                        else
                            lastSafe = middle;
                    }
                    float backoff = Mathf.Min(wallTurnBackoff, Vector2.Distance(from, lastSafe) * 0.25f);
                    return lastSafe - dir * backoff;
                }

                lastSafe = sample;
                if (clampedDistance >= maxDistance)
                    break;
            }

            return lastSafe;
        }

        bool FindFirstBeamPoint(Vector2 from, Vector2 to, out Vector2 beamPoint)
        {
            float distance = Vector2.Distance(from, to);
            if (distance <= 0.001f)
            {
                beamPoint = to;
                return false;
            }

            Vector2 dir = (to - from) / distance;
            for (float travelled = 0f; travelled <= distance; travelled += terrainProbeSpacing * 0.5f)
            {
                Vector2 sample = from + dir * Mathf.Min(travelled, distance);
                IntVector2 coord = room.GetTilePosition(sample);
                if (visitedBeamTiles.Contains(GetBeamTileKey(coord)))
                    continue;

                Room.Tile tile = room.GetTile(coord);
                if (!tile.AnyBeam)
                    continue;

                Vector2 center = room.MiddleOfTile(coord);
                float bestDistance = float.MaxValue;
                Vector2 projected = sample;

                // 将接触点投影到 Beam 中轴；交叉 Beam 则直接吸附到 Tile 中心。
                if (tile.horizontalBeam && tile.verticalBeam
                    && (Mathf.Abs(sample.y - center.y) <= beamCaptureRadius || Mathf.Abs(sample.x - center.x) <= beamCaptureRadius))
                {
                    beamPoint = center;
                    return true;
                }

                if (tile.horizontalBeam && Mathf.Abs(sample.y - center.y) <= beamCaptureRadius)
                {
                    bestDistance = Mathf.Abs(sample.y - center.y);
                    projected = new Vector2(sample.x, center.y);
                }

                if (tile.verticalBeam && Mathf.Abs(sample.x - center.x) <= beamCaptureRadius
                    && Mathf.Abs(sample.x - center.x) < bestDistance)
                {
                    projected = new Vector2(center.x, sample.y);
                    bestDistance = Mathf.Abs(sample.x - center.x);
                }

                if (bestDistance < float.MaxValue)
                {
                    beamPoint = projected;
                    return true;
                }
            }

            beamPoint = to;
            return false;
        }

        int GetBeamTileKey(IntVector2 coord)
        {
            return coord.y * room.TileWidth + coord.x;
        }

        BodyChunk FindFirstBodyChunk(Vector2 from, Vector2 to, out Vector2 hitPos)
        {
            float nearestTime = float.MaxValue;
            BodyChunk nearestChunk = null;
            hitPos = to;

            for (int i = 0; i < attractableChunks.Count; i++)
            {
                BodyChunk chunk = attractableChunks[i];
                // 同一个 BodyChunk 只命中一次，也不会继续对后续链路产生吸引。
                if (hitChunks.Contains(chunk))
                    continue;

                float collisionTime = Custom.CirclesCollisionTime(
                    from.x, from.y, chunk.pos.x, chunk.pos.y,
                    to.x - from.x, to.y - from.y,
                    bodyCollisionRadius, chunk.rad);

                if (collisionTime > 0.001f && collisionTime < 1f && collisionTime < nearestTime)
                {
                    nearestTime = collisionTime;
                    nearestChunk = chunk;
                }
            }

            if (nearestChunk != null)
                hitPos = Vector2.Lerp(from, to, nearestTime);
            return nearestChunk;
        }

        bool IsIgnoredElectricalObject(PhysicalObject obj)
        {
            return obj == source
                || obj is Player
                || obj is DMPSDroneCreature
                || obj is LegacyDroneCreature
                || !obj.canBeHitByWeapons;
        }

        float ConsumeTravelEnergy(Vector2 from, ref Vector2 to, float energy, out bool depleted)
        {
            depleted = false;
            float distance = Vector2.Distance(from, to);
            if (distance <= 0.001f)
                return energy;

            Vector2 dir = (to - from) / distance;
            Vector2 stepStart = from;

            // 按 Tile 介质分段积分能耗；若能量在步内耗尽，将终点截断到精确比例位置。
            for (float travelled = 0f; travelled < distance; travelled += logicSampleSpacing)
            {
                float stepLength = Mathf.Min(logicSampleSpacing, distance - travelled);
                Vector2 stepEnd = from + dir * (travelled + stepLength);
                Room.Tile tile = room.GetTile(Vector2.Lerp(stepStart, stepEnd, 0.5f));
                float cost = GetTileCost(tile) * (stepLength / logicSampleSpacing) * energyCostScale;

                if (cost >= energy)
                {
                    float fraction = cost <= 0f ? 0f : energy / cost;
                    to = Vector2.Lerp(stepStart, stepEnd, fraction);
                    depleted = true;
                    return 0f;
                }

                energy -= cost;
                stepStart = stepEnd;
            }

            return energy;
        }

        float GetTileCost(Room.Tile tile)
        {
            float cost = tile.AnyWater ? waterCost : 0f;
            cost += tile.Solid ? solidCost : airCost;
            if (tile.Terrain == Room.Tile.TerrainType.Slope)
                cost += slopeCost;
            if (tile.AnyBeam)
                cost += beamCost;
            return cost;
        }

        bool TryGetPreferredTargetDirection(Vector2 from, float currentDirection, out float targetDirection)
        {
            float bestScore = float.MaxValue;
            targetDirection = currentDirection;
            bool found = false;

            for (int i = 0; i < attractableChunks.Count; i++)
            {
                BodyChunk chunk = attractableChunks[i];
                if (hitChunks.Contains(chunk) || !room.VisualContact(from, chunk.pos))
                    continue;

                float direction = Custom.AimFromOneVectorToAnother(from, chunk.pos);
                float score = Vector2.Distance(from, chunk.pos)
                    + Mathf.Abs(Mathf.DeltaAngle(currentDirection, direction)) * 1.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    targetDirection = direction;
                    found = true;
                }
            }

            return found;
        }

        float GetBodyChunkAttraction(Vector2 from, Vector2 direction)
        {
            float attraction = 0f;
            for (int i = 0; i < attractableChunks.Count; i++)
            {
                BodyChunk chunk = attractableChunks[i];
                if (hitChunks.Contains(chunk))
                    continue;

                Vector2 toChunk = chunk.pos - from;
                float distance = toChunk.magnitude;
                if (distance <= 0.001f || distance >= bodyAttractionRange)
                    continue;

                float alignment = Vector2.Dot(direction, toChunk / distance);
                if (alignment <= 0f)
                    continue;

                float proximity = 1f - distance / bodyAttractionRange;
                attraction += Mathf.Pow(alignment, 3f) * proximity * bodyAttractionStrength;
            }

            return Mathf.Min(0.35f, attraction);
        }

        bool TryChooseLowestLossDirection(Vector2 from, float currentDirection, bool forceTurn, bool hasTarget, float targetDirection, float excludedDirection, out float bestDirection, out float bestTurnCost)
        {
            float bestScore = float.MaxValue;
            bestDirection = currentDirection;
            bestTurnCost = 0f;
            bool found = false;
            float noiseRange = Mathf.Min(35f, biasRange);
            float stochasticDirection = currentDirection + Random.Range(-noiseRange, noiseRange);

            // 在受限候选角中比较综合损耗：介质、转角、墙体，以及目标/BodyChunk/随机领头方向的收益。
            for (int i = 0; i < routeAngleOffsets.Length; i++)
            {
                float offset = routeAngleOffsets[i];
                if (Mathf.Abs(offset) > biasRange + 0.001f || (forceTurn && Mathf.Abs(offset) < 10f))
                    continue;

                float direction = currentDirection + offset;
                if (!float.IsNaN(excludedDirection) && Mathf.Abs(Mathf.DeltaAngle(excludedDirection, direction)) < 15f)
                    continue;

                Vector2 dir = Custom.DegToVec(direction);
                Vector2 probeEnd = TraceToTerrain(from, dir, routeLookAhead, out bool blocked);
                float clearDistance = Vector2.Distance(from, probeEnd);
                BodyChunk contactChunk = FindFirstBodyChunk(from, from + dir * routeLookAhead, out Vector2 contactPos);
                float contactDistance = contactChunk == null ? float.MaxValue : Vector2.Distance(from, contactPos);
                bool canContactBody = contactChunk != null
                    && (!blocked || contactDistance <= clearDistance + wallBodyHitTolerance);
                if (clearDistance < terrainProbeSpacing && !canContactBody)
                    continue;

                Vector2 costEnd = probeEnd;
                float projectedCost = 10f - ConsumeTravelEnergy(from, ref costEnd, 10f, out _);
                projectedCost *= routeLookAhead / Mathf.Max(terrainProbeSpacing, clearDistance);
                float turnCost = GetTurnEnergyCost(currentDirection, direction);
                float blockedPenalty = blocked ? (routeLookAhead - clearDistance) * 0.08f : 0f;
                if (canContactBody)
                    blockedPenalty *= 0.05f;
                float targetBonus = hasTarget
                    ? Mathf.Max(0f, Vector2.Dot(dir, Custom.DegToVec(targetDirection))) * 0.5f
                    : 0f;
                float contactBonus = canContactBody ? 0.75f : 0f;
                float bodyAttraction = GetBodyChunkAttraction(from, dir);
                float stochasticBonus = leaderNoise * (1f - Mathf.Clamp01(Mathf.Abs(Mathf.DeltaAngle(stochasticDirection, direction)) / 45f));
                float score = projectedCost + turnCost + blockedPenalty
                    - targetBonus - contactBonus - bodyAttraction - stochasticBonus
                    + Random.value * 0.04f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                    bestTurnCost = turnCost;
                    found = true;
                }
            }

            return found;
        }

        float GetTurnEnergyCost(float currentDirection, float nextDirection)
        {
            // 局部转折和偏离初始主方向分别收费，使闪电倾向保持惯性且不会无限漂离主轴。
            float localTurn = Mathf.Abs(Mathf.DeltaAngle(currentDirection, nextDirection));
            float mainDeviation = Mathf.Abs(Mathf.DeltaAngle(shootAngle, nextDirection));
            return localTurn * localTurnCostPerDegree + mainDeviation * mainDeviationCostPerDegree;
        }

        void GrowAlongBeam(PathState path, Queue<PathState> pendingBranches)
        {
            IntVector2 entry = room.GetTilePosition(path.pos);
            Room.Tile entryTile = room.GetTile(entry);
            List<BeamArm> arms = new List<BeamArm>(4);

            if (entryTile.horizontalBeam)
            {
                TryAddBeamArm(arms, entry, new IntVector2(-1, 0), true);
                TryAddBeamArm(arms, entry, new IntVector2(1, 0), true);
            }
            if (entryTile.verticalBeam)
            {
                TryAddBeamArm(arms, entry, new IntVector2(0, -1), false);
                TryAddBeamArm(arms, entry, new IntVector2(0, 1), false);
            }

            visitedBeamTiles.Add(GetBeamTileKey(entry));
            if (arms.Count == 0)
                return;

            // Beam 的所有连通臂共同分配入口能量，并把整段 Beam 登记为实际逻辑路径。
            float armEnergy = path.energy / arms.Count;
            for (int i = 0; i < arms.Count && logicSegments.Count < maxLogicalSegments; i++)
            {
                BeamArm arm = arms[i];
                float energyLoss = Vector2.Distance(path.pos, arm.end) / 20f * beamPathCostPerTile;
                float endEnergy = Mathf.Max(0f, armEnergy - energyLoss);
                AddBeamLogicSegment(path.pos, arm.end, armEnergy, endEnergy, path.branchDepth);
                TryCreateBeamBranch(path, arm, armEnergy, endEnergy, pendingBranches);
            }
        }

        void TryAddBeamArm(List<BeamArm> arms, IntVector2 entry, IntVector2 step, bool horizontal)
        {
            IntVector2 next = entry + step;
            if (!IsInsideRoom(next) || !HasBeam(room.GetTile(next), horizontal))
                return;

            IntVector2 current = entry;
            // 一次扫描到该方向 Beam 的末端；visited 集合避免之后从同一 Tile 重复导电。
            while (IsInsideRoom(current) && HasBeam(room.GetTile(current), horizontal))
            {
                visitedBeamTiles.Add(GetBeamTileKey(current));
                IntVector2 candidate = current + step;
                if (!IsInsideRoom(candidate) || !HasBeam(room.GetTile(candidate), horizontal))
                    break;
                current = candidate;
            }

            float direction = Custom.VecToDeg(new Vector2(step.x, step.y));
            arms.Add(new BeamArm(room.MiddleOfTile(current), direction));
        }

        bool HasBeam(Room.Tile tile, bool horizontal)
        {
            return horizontal ? tile.horizontalBeam : tile.verticalBeam;
        }

        bool IsInsideRoom(IntVector2 coord)
        {
            return coord.x >= 0 && coord.y >= 0 && coord.x < room.TileWidth && coord.y < room.TileHeight;
        }

        void AddBeamLogicSegment(Vector2 from, Vector2 to, float energyFrom, float energyTo, int branchDepth)
        {
            BodyChunk hitChunk = FindFirstBodyChunk(from, to, out Vector2 hitPos);
            if (hitChunk == null || Vector2.Distance(from, hitPos) < 0.5f || Vector2.Distance(hitPos, to) < 0.5f)
            {
                LogicSegment segment = new LogicSegment(from, to, energyFrom, energyTo, hitChunk, branchDepth);
                logicSegments.Add(segment);
                AddVisualSegment(segment);
                if (hitChunk != null)
                    hitChunks.Add(hitChunk);
                return;
            }

            // Beam 中途命中 BodyChunk 时拆成前后两段，保留完整 Beam 路径及命中点能量。
            float hitFactor = Mathf.InverseLerp(0f, Vector2.Distance(from, to), Vector2.Distance(from, hitPos));
            float hitEnergy = Mathf.Lerp(energyFrom, energyTo, hitFactor);
            LogicSegment beforeHit = new LogicSegment(from, hitPos, energyFrom, hitEnergy, hitChunk, branchDepth);
            logicSegments.Add(beforeHit);
            AddVisualSegment(beforeHit);
            hitChunks.Add(hitChunk);

            if (logicSegments.Count < maxLogicalSegments)
            {
                LogicSegment afterHit = new LogicSegment(hitPos, to, hitEnergy, energyTo, null, branchDepth);
                logicSegments.Add(afterHit);
                AddVisualSegment(afterHit);
            }
        }

        void TryCreateBeamBranch(PathState sourcePath, BeamArm arm, float energyFrom, float energyTo, Queue<PathState> pendingBranches)
        {
            float beamBranchChance = Mathf.Clamp01(0.55f + splitChance * 0.5f);
            int totalBranches = generatedRegularBranches + generatedBeamBranches;
            if (generatedBeamBranches >= maxBeamBranches || totalBranches >= maxTotalBranches || Random.value > beamBranchChance)
                return;

            // 支链可从 Beam 中部任意位置离开，起始能量取该位置的线性插值值。
            float positionFactor = Random.Range(0.15f, 0.85f);
            Vector2 branchPos = Vector2.Lerp(sourcePath.pos, arm.end, positionFactor);
            float localEnergy = Mathf.Lerp(energyFrom, energyTo, positionFactor);
            float side = Random.value < 0.5f ? -1f : 1f;
            float branchDirection = arm.direction + side * Random.Range(65f, 110f);
            float branchEnergy = Mathf.Max(localEnergy * 0.58f, sourcePath.energy * 0.22f)
                - GetTurnEnergyCost(arm.direction, branchDirection) * 0.25f;
            if (branchEnergy < minBranchEnergy)
                return;

            generatedBeamBranches++;
            pendingBranches.Enqueue(new PathState(branchPos, branchDirection, branchEnergy, Mathf.Max(0, sourcePath.splits - 1), false, sourcePath.branchDepth + 1));
        }

        void TryCreateBranch(ref PathState mainPath, float mainDirection, bool hasTarget, float targetDirection, Queue<PathState> pendingBranches)
        {
            float branchChance = Mathf.Clamp01(splitChance * (mainPath.branchDepth == 0 ? 1.5f : 0.8f));
            int totalBranches = generatedRegularBranches + generatedBeamBranches;
            if (generatedRegularBranches >= maxRegularBranches || totalBranches >= maxTotalBranches
                || mainPath.splits <= 0 || mainPath.energy < 1.2f || Random.value > branchChance)
                return;

            if (!TryChooseLowestLossDirection(mainPath.pos, mainPath.direction, true, hasTarget, targetDirection, mainDirection, out float branchDirection, out float branchTurnCost))
                return;

            // 分支不是复制能量：划走主链的一部分能量后，还要支付自己的转向损耗。
            float branchEnergy = mainPath.energy * branchEnergyShare - branchTurnCost;
            if (branchEnergy < minBranchEnergy)
                return;

            mainPath.energy *= 1f - branchEnergyShare;
            mainPath.splits--;
            generatedRegularBranches++;
            pendingBranches.Enqueue(new PathState(mainPath.pos, branchDirection, branchEnergy, mainPath.splits, false, mainPath.branchDepth + 1));
        }

        void AddVisualSegment(LogicSegment segment)
        {
            Vector2 visualFrom = segment.posFrom;
            float energyFrom = segment.energyFrom;
            float fullLength = Vector2.Distance(segment.posFrom, segment.posTo);
            // 发射点附近先保留短直线，确保视觉切线严格匹配 shootAngle，再开始中点位移。
            bool startsAtEmitter = segment.branchDepth == 0
                && Vector2.Distance(segment.posFrom, pos) <= 0.5f;
            if (startsAtEmitter && fullLength > visualInitialStraightLength + visualMinSegmentLength)
            {
                float straightFactor = visualInitialStraightLength / fullLength;
                visualFrom = Vector2.Lerp(segment.posFrom, segment.posTo, straightFactor);
                energyFrom = Mathf.Lerp(segment.energyFrom, segment.energyTo, straightFactor);
                AddConnection(segment.posFrom, visualFrom, segment.energyFrom, energyFrom);
            }

            float length = Vector2.Distance(visualFrom, segment.posTo);
            int depth = 0;
            float dividedLength = length;
            while (dividedLength > visualMinSegmentLength && depth < maxVisualDepth)
            {
                dividedLength *= 0.5f;
                depth++;
            }

            // 逻辑端点保持不动，只在端点之间生成更细的视觉折线。
            float amplitude = Mathf.Min(visualMaxOffset, length * visualOffsetFactor);
            AddMidpointDisplacedConnections(visualFrom, segment.posTo, energyFrom, segment.energyTo, depth, amplitude);
        }

        void AddMidpointDisplacedConnections(Vector2 from, Vector2 to, float energyFrom, float energyTo, int depth, float amplitude)
        {
            if (depth <= 0 || Vector2.Distance(from, to) <= visualMinSegmentLength)
            {
                AddConnection(from, to, energyFrom, energyTo);
                return;
            }

            Vector2 delta = to - from;
            Vector2 middle = (from + to) * 0.5f;
            Vector2 perpendicular = Custom.PerpendicularVector(delta.normalized);
            float offsetMagnitude = Mathf.Lerp(amplitude * visualMinOffsetFactor, amplitude, Random.value);
            float offset = (Random.value < 0.5f ? -1f : 1f) * offsetMagnitude;
            Vector2 displacedMiddle = middle + perpendicular * offset;

            // 位移点落入墙体时先尝试镜像侧，双侧都不可用才退回未位移中点。
            if (room.GetTile(displacedMiddle).Solid)
            {
                Vector2 opposite = middle - perpendicular * offset;
                displacedMiddle = room.GetTile(opposite).Solid ? middle : opposite;
            }

            // 每次细分同步插值能量，使亮度沿逻辑段连续衰减。
            float middleEnergy = Mathf.Lerp(energyFrom, energyTo, 0.5f);
            float nextAmplitude = amplitude * visualAmplitudeDecay;
            AddMidpointDisplacedConnections(from, displacedMiddle, energyFrom, middleEnergy, depth - 1, nextAmplitude);
            AddMidpointDisplacedConnections(displacedMiddle, to, middleEnergy, energyTo, depth - 1, nextAmplitude);
        }

        void AddConnection(Vector2 posFrom, Vector2 posTo, float energyFrom, float energyTo)
        {
            ShockConnection connection = new ShockConnection()
            {
                posFrom = posFrom,
                posTo = posTo,
                energyFrom = energyFrom,
                energyTo = energyTo,
                energy = (energyFrom + energyTo) * 0.5f
            };

            if(_container != null)
            {
                connection.CreateSprite();
                connection.AddSprites(_container);
            }

            shocks.Add(connection);
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

            if (!generated)
                GenerateLightning();
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            _container = new FContainer();
            foreach (var s in shocks)
            {
                s.CreateSprite();
                s.AddSprites(_container);
            }
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



        enum DecisionReason
        {
            EnergyDepleted,
            Obstacle,
            BodyChunk,
            Beam,
            LeaderPoint
        }

        readonly struct BeamArm
        {
            public readonly Vector2 end;
            public readonly float direction;

            public BeamArm(Vector2 end, float direction)
            {
                this.end = end;
                this.direction = direction;
            }
        }

        struct PathState
        {
            public Vector2 pos;
            public float direction;
            public float energy;
            public int splits;
            public bool firstSegment;
            public int decisions;
            public int branchDepth;

            public PathState(Vector2 pos, float direction, float energy, int splits, bool firstSegment, int branchDepth)
            {
                this.pos = pos;
                this.direction = direction;
                this.energy = energy;
                this.splits = splits;
                this.firstSegment = firstSegment;
                this.branchDepth = branchDepth;
                decisions = 0;
            }
        }

        readonly struct TravelResult
        {
            public readonly Vector2 pos;
            public readonly float energy;
            public readonly DecisionReason reason;
            public readonly BodyChunk hitChunk;

            public TravelResult(Vector2 pos, float energy, DecisionReason reason, BodyChunk hitChunk)
            {
                this.pos = pos;
                this.energy = energy;
                this.reason = reason;
                this.hitChunk = hitChunk;
            }
        }

        internal readonly struct LogicSegment
        {
            public readonly Vector2 posFrom;
            public readonly Vector2 posTo;
            public readonly float energyFrom;
            public readonly float energyTo;
            public readonly BodyChunk hitChunk;
            public readonly int branchDepth;

            public LogicSegment(Vector2 posFrom, Vector2 posTo, float energyFrom, float energyTo, BodyChunk hitChunk, int branchDepth)
            {
                this.posFrom = posFrom;
                this.posTo = posTo;
                this.energyFrom = energyFrom;
                this.energyTo = energyTo;
                this.hitChunk = hitChunk;
                this.branchDepth = branchDepth;
            }
        }

        public class ShockConnection
        {
            const float minScale = 1f, maxScale = 3.5f;

            public Vector2 posFrom, posTo;
            public float energy, energyFrom, energyTo;

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
