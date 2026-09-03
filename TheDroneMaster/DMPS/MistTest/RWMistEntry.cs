using System;
using System.IO;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;

namespace TheDroneMaster.DMPS.MistTest
{
    internal static class RWMistEntry
    {
        private const string VelocityShaderPath = "assets/myshader/rwmist/rwmistvelocity.compute";
        private const string InteractionShaderPath = "assets/myshader/rwmist/rwmistinteraction.compute";
        private const string DensityShaderPath = "assets/myshader/rwmist/rwmistdensity.compute";
        private const string RenderShaderPath = "assets/myshader/rwmist/rwmist.shader";
        internal const string RenderShaderName = "RWMist";

        public static ConditionalWeakTable<Room, MistRoom> mists = new ConditionalWeakTable<Room, MistRoom>();

        public static void HooksOn()
        {
            On.Room.Loaded += Room_Loaded;
            On.Room.Unloaded += Room_Unloaded;
            On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
        }

        private static void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
        {
            orig.Invoke(self, newRoom, cameraPosition);
            if(mists.TryGetValue(newRoom, out var mistRoom))
            {
                MistSimulation.Instance.InitializeRoom(mistRoom);
            }
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig.Invoke(self);
            if (MistSimulation.Instance == null || mists.TryGetValue(self, out _)) return;

            var mistRoom = MistRoom.Attach(self, MistSimulation.Instance);
            mists.Add(self, mistRoom);
        }

        private static void Room_Unloaded(On.Room.orig_Unloaded orig, Room self)
        {
            if (mists.TryGetValue(self, out var mistRoom))
            {
                mistRoom.Destroy();
                mists.Remove(self);
            }
            orig.Invoke(self);
        }

        public static void LoadResources()
        {
            if (MistSimulation.Instance != null) return;

            var bundlePath = AssetManager.ResolveFilePath("assetbundles/rwmist");
            if (!File.Exists(bundlePath))
                throw new FileNotFoundException("RWMist AssetBundle was not found.", bundlePath);

            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
                throw new InvalidOperationException($"Failed to load RWMist AssetBundle: {bundlePath}");

            try
            {
                var velocityShader = LoadRequired<ComputeShader>(bundle, VelocityShaderPath);
                var densityShader = LoadRequired<ComputeShader>(bundle, DensityShaderPath);
                var interactionShader = LoadRequired<ComputeShader>(bundle, InteractionShaderPath);
                var renderShader = LoadRequired<Shader>(bundle, RenderShaderPath);
                if (!Custom.rainWorld.Shaders.ContainsKey(RenderShaderName))
                    Custom.rainWorld.Shaders.Add(RenderShaderName,
                        FShader.CreateShader(RenderShaderName, renderShader));
                MistSimulation.GetOrCreate(velocityShader, densityShader, interactionShader);
            }
            finally
            {
                bundle.Unload(false);
            }
        }

        private static T LoadRequired<T>(AssetBundle bundle, string assetPath) where T : UnityEngine.Object
        {
            var asset = bundle.LoadAsset<T>(assetPath);
            if (asset == null)
                throw new InvalidOperationException(
                    $"Required RWMist asset '{assetPath}' was not found in the AssetBundle.");
            return asset;
        }

    }
}
