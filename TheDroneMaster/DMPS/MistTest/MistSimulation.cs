using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TheDroneMaster.DMPS.MistTest
{
    /// <summary>只负责在一个 <see cref="MistRoom"/> 的 GPU 资源上执行雾气物理模拟。</summary>
    internal sealed class MistSimulation : IDisposable
    {
        const int ThreadGroupSize = 8;

        private MistRoom activeRoom;
        private readonly ComputeShader velocityShader;
        private readonly ComputeShader densityShader;
        private readonly ComputeShader interactionShader;

        private ComputeBuffer interactionBuffer;

        //kernals
        int advectVelocityKernel, buoyancyKernel, divergenceKernel, clearPressureKernel, solvePressureKernel, projectVelocityKernel,
        advectDensityKernel, diffuseDensityKernel, finalizeDensityKernel, interactionKernel, clearInteractionFieldsKernel, rasterizeInteractionFieldKernel, applyInteractionFieldKernel;

        public static MistSimulation Instance { get; private set; }
        public bool IsInitialized => interactionBuffer != null;

        private RenderTexture CurrentDensity => activeRoom.DensityAIsCurrent ? activeRoom.DensityA : activeRoom.DensityB;
        private RenderTexture NextDensity => activeRoom.DensityAIsCurrent ? activeRoom.DensityB : activeRoom.DensityA;
        private RenderTexture CurrentVelocity => activeRoom.VelocityAIsCurrent ? activeRoom.VelocityA : activeRoom.VelocityB;
        private RenderTexture NextVelocity => activeRoom.VelocityAIsCurrent ? activeRoom.VelocityB : activeRoom.VelocityA;
        private RenderTexture CurrentPressure => activeRoom.PressureAIsCurrent ? activeRoom.PressureA : activeRoom.PressureB;
        private RenderTexture NextPressure => activeRoom.PressureAIsCurrent ? activeRoom.PressureB : activeRoom.PressureA;

        private MistSimulation(ComputeShader velocity, ComputeShader density, ComputeShader interaction)
        {
            if (!SystemInfo.supportsComputeShaders)
                throw new NotSupportedException("RWMist requires compute shader support.");
            velocityShader = velocity ?? throw new ArgumentNullException(nameof(velocity));
            densityShader = density ?? throw new ArgumentNullException(nameof(density));
            interactionShader = interaction ?? throw new ArgumentNullException(nameof(interaction));
            interactionBuffer = new ComputeBuffer(RWMistSettings.maximumInteractionsPerStep,
                Marshal.SizeOf(typeof(MistRoom.InteractionGpu)), ComputeBufferType.Structured);

            FindKernels();
        }
        public static MistSimulation GetOrCreate(ComputeShader velocity, ComputeShader density,
            ComputeShader interaction)
        {
            return Instance ??= new MistSimulation(velocity, density, interaction);
        }

        void FindKernels()
        {
            advectVelocityKernel = velocityShader.FindKernel("AdvectVelocity");
            buoyancyKernel = velocityShader.FindKernel("ApplyBuoyancy");
            divergenceKernel = velocityShader.FindKernel("ComputeDivergence");
            clearPressureKernel = velocityShader.FindKernel("ClearPressure");
            solvePressureKernel = velocityShader.FindKernel("SolvePressure");
            projectVelocityKernel = velocityShader.FindKernel("ProjectVelocity");
            advectDensityKernel = densityShader.FindKernel("AdvectDensity");
            diffuseDensityKernel = densityShader.FindKernel("DiffuseDensity");
            finalizeDensityKernel = densityShader.FindKernel("FinalizeDensity");
            interactionKernel = interactionShader.FindKernel("ApplyInteractions");
            clearInteractionFieldsKernel = interactionShader.FindKernel("ClearInteractionFields");
            rasterizeInteractionFieldKernel = interactionShader.FindKernel("RasterizeInteractionField");
            applyInteractionFieldKernel = interactionShader.FindKernel("ApplyInteractionField");
        }


        internal void InitializeRoom(MistRoom mistRoom)
        {
            RunForRoom(mistRoom, () => WriteConcentrationOutput(RWMistSettings.fixedTimeStep));
        }

        public void Step(MistRoom mistRoom, float deltaTime)
        {
            if (!IsInitialized || mistRoom == null || deltaTime <= 0f) return;
            RunForRoom(mistRoom, () => SimulateStep(Mathf.Min(deltaTime, 0.1f)));
        }

        private void RunForRoom(MistRoom mistRoom, Action action)
        {
            if (activeRoom != null)
                throw new InvalidOperationException("MistSimulation cannot execute two rooms at the same time.");
            activeRoom = mistRoom;
            try { action(); }
            finally { activeRoom = null; }
        }

        private void SimulateStep(float deltaTime)
        {
            // 顺序很重要：先更新流场并注入对象动量，投影为近似不可压缩流，再用最终速度搬运浓度。
            AdvectVelocity(deltaTime);
            ApplyBuoyancy(deltaTime);
            ApplyInteractions(deltaTime);
            ProjectVelocity(deltaTime);
            AdvectDensity(deltaTime);
            DiffuseDensity(deltaTime);
            WriteConcentrationOutput(deltaTime);
            activeRoom.PendingInteractions.Clear();
        }

        private void AdvectVelocity(float deltaTime)
        {
            SetCommon(velocityShader, deltaTime);
            velocityShader.SetFloat("_VelocityDissipation", RWMistSettings.velocityDissipation);
            velocityShader.SetTexture(advectVelocityKernel, "_VelocityRead", CurrentVelocity);
            velocityShader.SetTexture(advectVelocityKernel, "_VelocityWrite", NextVelocity);
            Dispatch(velocityShader, advectVelocityKernel);
            SwapVelocity();
        }

        private void ApplyBuoyancy(float deltaTime)
        {
            SetCommon(velocityShader, deltaTime);
            velocityShader.SetVector("_AmbientVelocity",
                new Vector4(RWMistSettings.ambientAcceleration.x, RWMistSettings.ambientAcceleration.y, 0f, 0f));
            velocityShader.SetFloat("_Buoyancy", RWMistSettings.buoyancy);
            velocityShader.SetFloat("_FogWeight", RWMistSettings.fogWeight);
            velocityShader.SetTexture(buoyancyKernel, "_DensityRead", CurrentDensity);
            velocityShader.SetTexture(buoyancyKernel, "_VelocityRead", CurrentVelocity);
            velocityShader.SetTexture(buoyancyKernel, "_VelocityWrite", NextVelocity);
            Dispatch(velocityShader, buoyancyKernel);
            SwapVelocity();
        }

        private void ApplyInteractions(float deltaTime)
        {
            activeRoom.LastInteractionCount = activeRoom.PendingInteractions.Count;
            activeRoom.LastInteractionUsedField = false;
            if (activeRoom.PendingInteractions.Count == 0)
            {
                if (activeRoom.InteractionFieldTimeToLive > 0f)
                {
                    // 没有新对象时仍应用衰减后的速度尾迹；浓度和排开通道已经在准备阶段清零。
                    ApplyInteractionsThroughField(Array.Empty<MistRoom.InteractionGpu>(), deltaTime);
                    activeRoom.InteractionFieldTimeToLive -= deltaTime;
                    activeRoom.LastInteractionUsedField = true;
                    if (activeRoom.InteractionFieldTimeToLive <= 0f)
                        ClearInteractionFields(deltaTime, true);
                }
                else if (activeRoom.InteractionFieldContainsData)
                {
                    ClearInteractionFields(deltaTime, true);
                }
                return;
            }
            // C# InteractionGpu 与 HLSL RWMistInteraction 都是连续的 10 个 float（40 字节），字段顺序不可随意更改。
            var interactionData = activeRoom.PendingInteractions.ToArray();
            interactionBuffer.SetData(interactionData, 0, 0, interactionData.Length);

            if (interactionData.Length > RWMistSettings.directInteractionThreshold ||
                activeRoom.InteractionFieldTimeToLive > 0f)
            {
                ApplyInteractionsThroughField(interactionData, deltaTime);
                // 十个半衰期后残留小于千分之一，此后直接清空可避免永久增加一次全图 dispatch。
                activeRoom.InteractionFieldTimeToLive = RWMistSettings.bodyVelocityFieldHalfLife * 10f;
                activeRoom.LastInteractionUsedField = true;
                return;
            }

            if (activeRoom.InteractionFieldContainsData)
                ClearInteractionFields(deltaTime, true);
            SetCommon(interactionShader, deltaTime);
            interactionShader.SetInt("_InteractionCount", interactionData.Length);
            interactionShader.SetBuffer(interactionKernel, "_Interactions", interactionBuffer);
            interactionShader.SetTexture(interactionKernel, "_DensityRead", CurrentDensity);
            interactionShader.SetTexture(interactionKernel, "_DensityWrite", NextDensity);
            interactionShader.SetTexture(interactionKernel, "_VelocityRead", CurrentVelocity);
            interactionShader.SetTexture(interactionKernel, "_VelocityWrite", NextVelocity);
            Dispatch(interactionShader, interactionKernel);
            SwapDensity();
            SwapVelocity();
        }

        private void ApplyInteractionsThroughField(MistRoom.InteractionGpu[] interactions, float deltaTime)
        {
            var halfLife = Mathf.Max(RWMistSettings.bodyVelocityFieldHalfLife, 0.01f);
            var decay = Mathf.Pow(0.5f, deltaTime / halfLife);
            ClearInteractionFields(deltaTime, false);
            SetCommon(interactionShader, deltaTime);
            interactionShader.SetFloat("_VelocityFieldDecay", decay);
            interactionShader.SetBuffer(rasterizeInteractionFieldKernel, "_Interactions", interactionBuffer);
            interactionShader.SetTexture(rasterizeInteractionFieldKernel, "_BodyVelocityAccumulation",
                activeRoom.BodyVelocityAccumulation);
            interactionShader.SetTexture(rasterizeInteractionFieldKernel, "_BodyEffectField",
                activeRoom.BodyEffectField);

            var width = CurrentDensity.width;
            var height = CurrentDensity.height;
            for (var index = 0; index < interactions.Length; index++)
            {
                var interaction = interactions[index];
                // 只栅格化圆形影响范围的包围盒；房间越大，相对逐格遍历的收益越明显。
                // 包围起点和终点，GPU 再计算点到线段的距离，从而得到连续的扫掠胶囊体。
                var minX = Mathf.Max(0, Mathf.FloorToInt(
                    Mathf.Min(interaction.previousCenter.x, interaction.center.x) - interaction.radius - 1f));
                var minY = Mathf.Max(0, Mathf.FloorToInt(
                    Mathf.Min(interaction.previousCenter.y, interaction.center.y) - interaction.radius - 1f));
                var maxX = Mathf.Min(width, Mathf.CeilToInt(
                    Mathf.Max(interaction.previousCenter.x, interaction.center.x) + interaction.radius + 1f));
                var maxY = Mathf.Min(height, Mathf.CeilToInt(
                    Mathf.Max(interaction.previousCenter.y, interaction.center.y) + interaction.radius + 1f));
                var rasterWidth = maxX - minX;
                var rasterHeight = maxY - minY;
                if (rasterWidth <= 0 || rasterHeight <= 0) continue;

                interactionShader.SetInt("_RasterInteractionIndex", index);
                interactionShader.SetInts("_RasterOffset", minX, minY);
                interactionShader.SetInts("_RasterSize", rasterWidth, rasterHeight);
                interactionShader.Dispatch(rasterizeInteractionFieldKernel,
                    Mathf.CeilToInt(rasterWidth / (float)ThreadGroupSize),
                    Mathf.CeilToInt(rasterHeight / (float)ThreadGroupSize), 1);
            }

            interactionShader.SetTexture(applyInteractionFieldKernel, "_DensityRead", CurrentDensity);
            interactionShader.SetTexture(applyInteractionFieldKernel, "_DensityWrite", NextDensity);
            interactionShader.SetTexture(applyInteractionFieldKernel, "_VelocityRead", CurrentVelocity);
            interactionShader.SetTexture(applyInteractionFieldKernel, "_VelocityWrite", NextVelocity);
            interactionShader.SetTexture(applyInteractionFieldKernel, "_BodyVelocityField",
                activeRoom.BodyVelocityField);
            interactionShader.SetTexture(applyInteractionFieldKernel, "_BodyVelocityAccumulation",
                activeRoom.BodyVelocityAccumulation);
            interactionShader.SetTexture(applyInteractionFieldKernel, "_BodyEffectField", activeRoom.BodyEffectField);
            Dispatch(interactionShader, applyInteractionFieldKernel);
            SwapDensity();
            SwapVelocity();
            activeRoom.InteractionFieldContainsData = true;
        }

        private void ClearInteractionFields(float deltaTime, bool clearPersistentVelocity)
        {
            SetCommon(interactionShader, deltaTime);
            interactionShader.SetInt("_ClearPersistentVelocity", clearPersistentVelocity ? 1 : 0);
            interactionShader.SetTexture(clearInteractionFieldsKernel, "_BodyVelocityField",
                activeRoom.BodyVelocityField);
            interactionShader.SetTexture(clearInteractionFieldsKernel, "_BodyVelocityAccumulation",
                activeRoom.BodyVelocityAccumulation);
            interactionShader.SetTexture(clearInteractionFieldsKernel, "_BodyEffectField", activeRoom.BodyEffectField);
            Dispatch(interactionShader, clearInteractionFieldsKernel);
            activeRoom.InteractionFieldContainsData = !clearPersistentVelocity;
        }

        private void ProjectVelocity(float deltaTime)
        {
            // Helmholtz-Hodge 投影：散度 -> 压力泊松方程 -> 减去压力梯度。
            SetCommon(velocityShader, deltaTime);
            velocityShader.SetTexture(divergenceKernel, "_VelocityRead", CurrentVelocity);
            velocityShader.SetTexture(divergenceKernel, "_Divergence", activeRoom.Divergence);
            Dispatch(velocityShader, divergenceKernel);

            // 每个时间步重新求压力，必须把两张迭代纹理都清零，避免上一帧残留影响收敛。
            velocityShader.SetTexture(clearPressureKernel, "_PressureWrite", activeRoom.PressureA);
            Dispatch(velocityShader, clearPressureKernel);
            velocityShader.SetTexture(clearPressureKernel, "_PressureWrite", activeRoom.PressureB);
            Dispatch(velocityShader, clearPressureKernel);
            activeRoom.PressureAIsCurrent = true;

            velocityShader.SetFloat("_PressureRelaxation", RWMistSettings.pressureRelaxation);
            velocityShader.SetTexture(solvePressureKernel, "_Divergence", activeRoom.Divergence);
            for (var iteration = 0; iteration < RWMistSettings.pressureIterations; iteration++)
            {
                // Jacobi 每次只能读取上一轮结果，因此压力纹理也需要 Ping-Pong。
                velocityShader.SetTexture(solvePressureKernel, "_PressureRead", CurrentPressure);
                velocityShader.SetTexture(solvePressureKernel, "_PressureWrite", NextPressure);
                Dispatch(velocityShader, solvePressureKernel);
                activeRoom.PressureAIsCurrent = !activeRoom.PressureAIsCurrent;
            }

            velocityShader.SetTexture(projectVelocityKernel, "_PressureRead", CurrentPressure);
            velocityShader.SetTexture(projectVelocityKernel, "_VelocityRead", CurrentVelocity);
            velocityShader.SetTexture(projectVelocityKernel, "_VelocityWrite", NextVelocity);
            Dispatch(velocityShader, projectVelocityKernel);
            SwapVelocity();
        }

        private void AdvectDensity(float deltaTime)
        {
            SetCommon(densityShader, deltaTime);
            densityShader.SetTexture(advectDensityKernel, "_DensityRead", CurrentDensity);
            densityShader.SetTexture(advectDensityKernel, "_DensityWrite", NextDensity);
            densityShader.SetTexture(advectDensityKernel, "_VelocityRead", CurrentVelocity);
            Dispatch(densityShader, advectDensityKernel);
            SwapDensity();
        }

        private void DiffuseDensity(float deltaTime)
        {
            densityShader.SetFloat("_DensityDiffusion", RWMistSettings.densityDiffusion);
            for (var iteration = 0; iteration < RWMistSettings.densityIterations; iteration++)
            {
                SetCommon(densityShader, deltaTime / Mathf.Max(1, RWMistSettings.densityIterations));
                densityShader.SetTexture(diffuseDensityKernel, "_DensityRead", CurrentDensity);
                densityShader.SetTexture(diffuseDensityKernel, "_DensityWrite", NextDensity);
                Dispatch(densityShader, diffuseDensityKernel);
                SwapDensity();
            }
        }

        private void WriteConcentrationOutput(float deltaTime)
        {
            // concentrationOutput 始终是同一张 RT；外部材质无需跟随内部 density A/B 翻转重新绑定。
            SetCommon(densityShader, deltaTime);
            densityShader.SetFloat("_DensityDissipation", RWMistSettings.densityDissipation);
            densityShader.SetFloat("_MaximumDensity", RWMistSettings.maximumDensity);
            densityShader.SetTexture(finalizeDensityKernel, "_DensityRead", CurrentDensity);
            densityShader.SetTexture(finalizeDensityKernel, "_DensityWrite", NextDensity);
            densityShader.SetTexture(finalizeDensityKernel, "_ConcentrationOutput", activeRoom.ConcentrationOutput);
            Dispatch(densityShader, finalizeDensityKernel);
            SwapDensity();
        }

        private void SetCommon(ComputeShader shader, float deltaTime)
        {
            shader.SetInts("_GridSize", CurrentDensity.width, CurrentDensity.height);
            shader.SetInts("_RoomTileSize", activeRoom.Grid.Width, activeRoom.Grid.Height);
            shader.SetInt("_CellsPerTile", RWMistSettings.cellsPerRoomTile);
            shader.SetFloat("_InverseCellsPerTile", 1f / RWMistSettings.cellsPerRoomTile);
            shader.SetFloat("_CellSize", TheDroneMaster.DMPS.MistTest.RoomGrid.TileSize /
                RWMistSettings.cellsPerRoomTile);
            shader.SetFloat("_DeltaTime", deltaTime);
            BindRoomMask(shader);
        }

        private void BindRoomMask(ComputeShader shader)
        {
            if (shader == velocityShader)
            {
                BindMask(shader, advectVelocityKernel, buoyancyKernel, divergenceKernel, solvePressureKernel,
                    projectVelocityKernel);
            }
            else if (shader == densityShader)
            {
                BindMask(shader, advectDensityKernel, diffuseDensityKernel, finalizeDensityKernel);
            }
            else
            {
                BindMask(shader, interactionKernel, clearInteractionFieldsKernel,
                    rasterizeInteractionFieldKernel, applyInteractionFieldKernel);
            }
        }

        private void BindMask(ComputeShader shader, params int[] kernels)
        {
            foreach (var kernel in kernels)
                shader.SetTexture(kernel, "_RoomGeometry", activeRoom.GeometryMask);
        }


        private void Dispatch(ComputeShader shader, int kernel)
        {
            shader.Dispatch(kernel, Mathf.CeilToInt(CurrentDensity.width / (float)ThreadGroupSize),
                Mathf.CeilToInt(CurrentDensity.height / (float)ThreadGroupSize), 1);
        }

        private void SwapDensity() => activeRoom.DensityAIsCurrent = !activeRoom.DensityAIsCurrent;
        private void SwapVelocity() => activeRoom.VelocityAIsCurrent = !activeRoom.VelocityAIsCurrent;

        public void Dispose()
        {
            interactionBuffer?.Release();
            interactionBuffer = null;
            activeRoom = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

    }
}
