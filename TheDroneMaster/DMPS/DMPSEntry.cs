using Fisobs.Core;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPShud;
using TheDroneMaster.DMPS.MenuHooks;
using TheDroneMaster.DMPS.MenuHooks.KarmaLadderScreenHooks;
using TheDroneMaster.DMPS.PlayerHooks;
using UnityEngine;

namespace TheDroneMaster.DMPS
{
    internal static class DMPSEntry
    {
        public static void OnModInit()
        {
            DMPSPlayerHooks.HooksOn();
            DMPSHUDHooks.HooksOn();
            KarmaLadderScreenHooks.HooksOn();
            MenuHooks.MenuHooks.HooksOn();
            Save.HooksOn();
        }

        public static void LoadResources()
        {
            string path = AssetManager.ResolveFilePath("assetbundles/dmps");
            AssetBundle ab = AssetBundle.LoadFromFile(path);


            Custom.rainWorld.Shaders.Add("AdditiveDefault", FShader.CreateShader("AdditiveDefault", ab.LoadAsset<Shader>("assets/myshader/dronemaster/additivedefault.shader")));

            ab.Unload(false);

            Futile.atlasManager.LoadAtlasFromTexture("DMPS_JetFlare", LoadTexFromPath("illustrations/DMPS_JetFlare.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_BioReactor", LoadTexFromPath("illustrations/DMPS_BioReactor.png"), false); 
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_BioReactorFlare", LoadTexFromPath("illustrations/DMPS_BioReactorFlare.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_SkillRect", LoadTexFromPath("illustrations/DMPS_SkillRect.png"), false);
        }

        public static Texture2D LoadTexFromPath(string path)
        {
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false);
            return AssetManager.SafeWWWLoadTexture(ref texture2D, AssetManager.ResolveFilePath(path), clampWrapMode: false, crispPixels: true);
        }

        public static void RegisterFisobs()
        {
            Content.Register(new DMPSDrone.DMPSDroneCritob());
        }
    }
}
