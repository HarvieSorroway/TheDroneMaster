using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DMPS.PlayerHooks;
using UnityEngine;

namespace TheDroneMaster.DMPS.MistTest
{
    /// <summary>与一个已实现房间绑定的雾气网格、GPU 纹理和每房间模拟状态。</summary>
    internal sealed class MistRoom : UpdatableAndDeletable, IDrawable
    {
        private const int MistSprite = 0;
        private const int DebugSprite = 1;
        private const float DebugWindowWidth = 320f;
        private const float DebugWindowHeight = 180f;
        private const float DebugWindowMargin = 20f;
        private static readonly RWMistPhysicalObjectProfile BodyChunkProfile = new RWMistPhysicalObjectProfile
        {
            radiusMultiplier = 1f,
            radiusPadding = 4f,
            velocityCoupling = 12f,
            displacement = 5f,
            densityPerSecond = 0f
        };

        private bool disposed;

        public RoomGrid Grid { get; }
        public MistSimulation Simulation { get; }
        internal readonly List<InteractionGpu> PendingInteractions = new List<InteractionGpu>();
        internal bool DensityAIsCurrent = true;
        internal bool VelocityAIsCurrent = true;
        internal bool PressureAIsCurrent = true;
        internal bool InteractionFieldContainsData;
        internal float InteractionFieldTimeToLive;
        public bool LastInteractionUsedField { get; internal set; }
        public int LastInteractionCount { get; internal set; }

        internal RenderTexture DensityA { get; }
        internal RenderTexture DensityB { get; }
        internal RenderTexture VelocityA { get; }
        internal RenderTexture VelocityB { get; }
        internal RenderTexture PressureA { get; }
        internal RenderTexture PressureB { get; }
        internal RenderTexture Divergence { get; }
        internal RenderTexture ConcentrationOutput { get; }
        internal RenderTexture BodyVelocityField { get; }
        internal RenderTexture BodyVelocityAccumulation { get; }
        internal RenderTexture BodyEffectField { get; }
        internal Texture2D GeometryMask { get; }

        public RenderTexture ConcentrationTexture => ConcentrationOutput;
        public RenderTexture VelocityTexture => VelocityAIsCurrent ? VelocityA : VelocityB;
        public Vector4 WorldToUvTransform
        {
            get
            {
                var worldSize = new Vector2(Grid.Width * RoomGrid.TileSize, Grid.Height * RoomGrid.TileSize);
                return new Vector4(1f / worldSize.x, 1f / worldSize.y,
                    -Grid.WorldOrigin.x / worldSize.x, -Grid.WorldOrigin.y / worldSize.y);
            }
        }

        private MistRoom(Room room, MistSimulation simulation)
        {
            this.room = room ?? throw new ArgumentNullException(nameof(room));
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            Grid = new RoomGrid(room);
            if (Grid.Width <= 0 || Grid.Height <= 0)
                throw new InvalidOperationException(
                    $"Cannot create RWMist resources before room '{room.abstractRoom.name}' terrain is loaded.");
            if (Grid.HasSlopes && RWMistSettings.cellsPerRoomTile < 2)
                throw new InvalidOperationException("Slope collision requires at least 2 simulation cells per room tile.");

            var width = Grid.Width * RWMistSettings.cellsPerRoomTile;
            var height = Grid.Height * RWMistSettings.cellsPerRoomTile;
            DensityA = CreateRenderTexture(width, height, RenderTextureFormat.RFloat, $"RWMist {room.abstractRoom.name} Density A");
            DensityB = CreateRenderTexture(width, height, RenderTextureFormat.RFloat, $"RWMist {room.abstractRoom.name} Density B");
            VelocityA = CreateRenderTexture(width, height, RenderTextureFormat.RGFloat, $"RWMist {room.abstractRoom.name} Velocity A");
            VelocityB = CreateRenderTexture(width, height, RenderTextureFormat.RGFloat, $"RWMist {room.abstractRoom.name} Velocity B");
            PressureA = CreateRenderTexture(width, height, RenderTextureFormat.RFloat, $"RWMist {room.abstractRoom.name} Pressure A");
            PressureB = CreateRenderTexture(width, height, RenderTextureFormat.RFloat, $"RWMist {room.abstractRoom.name} Pressure B");
            Divergence = CreateRenderTexture(width, height, RenderTextureFormat.RFloat, $"RWMist {room.abstractRoom.name} Divergence");
            ConcentrationOutput = CreateRenderTexture(width, height, RenderTextureFormat.RFloat,
                $"RWMist {room.abstractRoom.name} Concentration Output");
            BodyVelocityField = CreateRenderTexture(width, height, RenderTextureFormat.ARGBFloat,
                $"RWMist {room.abstractRoom.name} BodyChunk Velocity Field");
            BodyVelocityAccumulation = CreateRenderTexture(width, height, RenderTextureFormat.ARGBFloat,
                $"RWMist {room.abstractRoom.name} BodyChunk Velocity Accumulation");
            BodyEffectField = CreateRenderTexture(width, height, RenderTextureFormat.ARGBFloat,
                $"RWMist {room.abstractRoom.name} BodyChunk Effect Field");
            GeometryMask = CreateRoomGeometry(Grid);

            ClearTextures();
            //Simulation.InitializeRoom(this);
        }

        public static MistRoom Attach(Room room, MistSimulation simulation)
        {
            var mistRoom = new MistRoom(room, simulation);
            room.AddObject(mistRoom);
            return mistRoom;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!disposed && room == room.game.cameras[0].room)
            {
                QueueJetMistSources(RWMistSettings.fixedTimeStep);
                QueueBodyChunks(RWMistSettings.fixedTimeStep);
                Simulation.Step(this, RWMistSettings.fixedTimeStep);
            }
        }

        private void QueueJetMistSources(float deltaTime)
        {
            foreach (var layer in room.physicalObjects)
            foreach (var physicalObject in layer)
            {
                if (!(physicalObject is Player player) || physicalObject.slatedForDeletetion ||
                    !TheDroneMaster.PlayerPatchs.TryGetModule<DMPSModule>(player, out var module) ||
                    !module.IsJetJumping)
                    continue;

                var chunk = player.mainBodyChunk;
                var safeDeltaTime = Mathf.Max(deltaTime, 0.0001f);
                var playerVelocity = (chunk.pos - chunk.lastPos) / safeDeltaTime;
                if (playerVelocity.sqrMagnitude < 0.0001f)
                    playerVelocity = chunk.vel / safeDeltaTime;

                QueueMistSource(chunk.pos,
                    -playerVelocity * RWMistSettings.jetMistVelocityMultiplier,
                    RWMistSettings.jetMistRadius, RWMistSettings.jetMistDensityPerSecond,
                    RWMistSettings.jetMistVelocityCoupling);
            }
        }

        private void QueueBodyChunks(float deltaTime)
        {
            foreach (var layer in room.physicalObjects)
            foreach (var physicalObject in layer)
            {
                if (physicalObject == null || physicalObject.slatedForDeletetion ||
                    physicalObject.bodyChunks == null)
                    continue;

                foreach (var chunk in physicalObject.bodyChunks)
                {
                    if (chunk == null) continue;
                    QueuePhysicalObject(chunk.pos, chunk.lastPos, chunk.rad, BodyChunkProfile, deltaTime);
                }
            }
        }

        public void QueuePhysicalObject(Vector2 position, Vector2 previousPosition, float bodyChunkRadius,
            RWMistPhysicalObjectProfile profile, float gameDeltaTime)
        {
            if (profile == null) return;
            // BodyChunk 本身保持雨世界坐标单位；统一在 QueueInteraction 中换算为模拟格单位。
            var radius = bodyChunkRadius * profile.radiusMultiplier + profile.radiusPadding;
            QueueInteraction(position, previousPosition, radius, profile.densityPerSecond,
                profile.velocityCoupling, profile.displacement, gameDeltaTime);
        }

        public void QueueInteraction(Vector2 position, Vector2 previousPosition, float radius,
            float densityPerSecond, float velocityCoupling, float displacement, float gameDeltaTime)
        {
            if (disposed || radius <= 0f ||
                PendingInteractions.Count >= RWMistSettings.maximumInteractionsPerStep)
                return;

            var cellSize = RoomGrid.TileSize / RWMistSettings.cellsPerRoomTile;
            var center = (position - Grid.WorldOrigin) / cellSize;
            var safeDeltaTime = Mathf.Max(gameDeltaTime, 0.0001f);
            PendingInteractions.Add(new InteractionGpu
            {
                center = center,
                previousCenter = (previousPosition - Grid.WorldOrigin) / cellSize,
                // pos-lastPos 与此次提交的 gameDeltaTime 严格对应，比直接使用 chunk.vel 更稳定。
                motionVelocity = (position - previousPosition) / (cellSize * safeDeltaTime),
                radius = radius / cellSize,
                densityRate = densityPerSecond,
                velocityCoupling = velocityCoupling,
                displacement = displacement
            });
        }

        public void QueueMistSource(Vector2 position, Vector2 worldVelocity, float radius,
            float densityPerSecond, float velocityCoupling)
        {
            if (disposed || radius <= 0f ||
                PendingInteractions.Count >= RWMistSettings.maximumInteractionsPerStep)
                return;

            var cellSize = RoomGrid.TileSize / RWMistSettings.cellsPerRoomTile;
            var center = (position - Grid.WorldOrigin) / cellSize;
            PendingInteractions.Add(new InteractionGpu
            {
                center = center,
                // 雾源固定在当前位置；喷射方向通过独立速度指定，不生成额外扫掠段。
                previousCenter = center,
                motionVelocity = worldVelocity / cellSize,
                radius = radius / cellSize,
                densityRate = densityPerSecond,
                velocityCoupling = velocityCoupling,
                displacement = 0f
            });
        }

        public void BindOutput(Material material, string textureProperty = "_RWMistConcentration",
            string transformProperty = "_RWMistWorldToUV")
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            material.SetTexture(textureProperty, ConcentrationOutput);
            material.SetVector(transformProperty, WorldToUvTransform);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            var worldScale = RoomGrid.TileSize / RWMistSettings.cellsPerRoomTile;
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[MistSprite] = new FTexture(ConcentrationTexture)
            {
                anchorX = 0f,
                anchorY = 0f,
                scaleX = worldScale,
                scaleY = worldScale,
                color = Color.white,
                shader = rCam.game.rainWorld.Shaders[RWMistEntry.RenderShaderName]
            };
            sLeaser.sprites[DebugSprite] = new FTexture(ConcentrationTexture)
            {
                anchorX = 0f,
                anchorY = 1f,
                scaleX = DebugWindowWidth / ConcentrationTexture.width,
                scaleY = DebugWindowHeight / ConcentrationTexture.height,
                x = DebugWindowMargin,
                y = rCam.sSize.y - DebugWindowMargin,
                color = Color.white
            };
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker,
            Vector2 camPos)
        {
            if (!sLeaser.deleteMeNextFrame && (slatedForDeletetion || disposed || room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }

            var mist = sLeaser.sprites[MistSprite];
            mist.x = Grid.WorldOrigin.x - camPos.x;
            mist.y = Grid.WorldOrigin.y - camPos.y;

            // HUD 坐标不减相机位置；只在分辨率变化时随屏幕左上角移动。
            var preview = sLeaser.sprites[DebugSprite];
            preview.x = DebugWindowMargin;
            preview.y = rCam.sSize.y - DebugWindowMargin;
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer)
        {
            var mistContainer = newContainer ?? rCam.ReturnFContainer("Water");
            sLeaser.sprites[MistSprite].RemoveFromContainer();
            mistContainer.AddChild(sLeaser.sprites[MistSprite]);

            sLeaser.sprites[DebugSprite].RemoveFromContainer();
            rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[DebugSprite]);
        }

        public override void Destroy()
        {
            if (disposed) return;
            disposed = true;
            Release(DensityA);
            Release(DensityB);
            Release(VelocityA);
            Release(VelocityB);
            Release(PressureA);
            Release(PressureB);
            Release(Divergence);
            Release(ConcentrationOutput);
            Release(BodyVelocityField);
            Release(BodyVelocityAccumulation);
            Release(BodyEffectField);
            UnityEngine.Object.Destroy(GeometryMask);
            base.Destroy();
        }

        private void ClearTextures()
        {
            var previous = RenderTexture.active;
            var textures = new[]
            {
                DensityA, DensityB, VelocityA, VelocityB, PressureA, PressureB, Divergence,
                ConcentrationOutput, BodyVelocityField, BodyVelocityAccumulation, BodyEffectField
            };
            foreach (var texture in textures)
            {
                RenderTexture.active = texture;
                GL.Clear(false, true, Color.clear);
            }
            RenderTexture.active = previous;
        }

        private static RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format,
            string name)
        {
            var texture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear)
            {
                name = name,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            texture.Create();
            return texture;
        }

        private static Texture2D CreateRoomGeometry(RoomGrid grid)
        {
            var texture = new Texture2D(grid.Width, grid.Height, TextureFormat.R8, false, true)
            {
                name = $"RWMist {grid.Room.abstractRoom.name} Room Collision Geometry",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[grid.Width * grid.Height];
            for (var y = 0; y < grid.Height; y++)
            for (var x = 0; x < grid.Width; x++)
            {
                var value = grid.GetCollisionGeometry(x, y);
                pixels[x + y * grid.Width] = new Color32(value, value, value, 255);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void Release(RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release();
            UnityEngine.Object.Destroy(texture);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct InteractionGpu
        {
            // 与 HLSL RWMistInteraction 保持二进制一致：连续 10 个 float，共 40 字节。
            public Vector2 center;
            public Vector2 previousCenter;
            public Vector2 motionVelocity;
            public float radius;
            public float densityRate;
            public float velocityCoupling;
            public float displacement;
        }

    }
}
