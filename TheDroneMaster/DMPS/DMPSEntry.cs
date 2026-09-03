using CustomSaveTx;
using Fisobs.Core;
using Newtonsoft.Json;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSDynamicParams;
using TheDroneMaster.DMPS.DMPSGameHooks;
using TheDroneMaster.DMPS.DMPShud;
using TheDroneMaster.DMPS.DMPSSave;
using TheDroneMaster.DMPS.DMPSSkillTree;
using TheDroneMaster.DMPS.GameHooks;
using TheDroneMaster.DMPS.MenuHooks;
using TheDroneMaster.DMPS.MenuHooks.KarmaLadderScreenHooks;
using TheDroneMaster.DMPS.MistTest;
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
            MenuHooks.MenuHooks.HooksOn();
            //Save.HooksOn();
            DMPSDynamicParamHooks.HooksOn();
            RenderNodeLoader.Load();
            SkillNodeLoader.Load();
            SkillTreeHooks.HooksOn();
            TheDroneMaster.DMPS.DMPSGameHooks.GameHooks.HooksOn();
            DMPSGamePatch.ShieldPatchOn();
            DeathPersistentSaveDataRx.AppplyTreatment(new DMPSBasicSave(null));

            RWMistEntry.HooksOn();
            
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
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_PixelGradiant20", LoadTexFromPath("illustrations/DMPS_PixelGradiant20.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_RectLightLeft", LoadTexFromPath("illustrations/rectlight_left.png"), false);


            Futile.atlasManager.LoadAtlasFromTexture("SkillScreen_IconBkg", LoadTexFromPath("illustrations/skillscreen_iconbkg.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("SkillScreen_IconBkg_2", LoadTexFromPath("illustrations/skillscreen_iconbkg_2.png"), false);

            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.Base", LoadTexFromPath("illustrations/rendernode_base.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.DronePortUpg", LoadTexFromPath("illustrations/rendernode_droneportupg.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.DroneUpg", LoadTexFromPath("illustrations/rendernode_droneupg.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.ReactorUpg", LoadTexFromPath("illustrations/rendernode_reactorupg.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.TorsoUpg", LoadTexFromPath("illustrations/rendernode_torsoupg.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.PlaceHolder", LoadTexFromPath("illustrations/rendernode_placeholder.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.Esc", LoadTexFromPath("illustrations/rendernode_esc.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.DroneUpg.Dmg", LoadTexFromPath("illustrations/rendernode_droneupg_dmg.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("RenderNode.DroneUpg.Count", LoadTexFromPath("illustrations/rendernode_droneupg_count.png"), false);

            LoadOverDriveBarResources();
            LoadFeedBackBarResources();
            LoadThunderBoltBarResources();
            LoadShockEffectResources();
            DMPSResourceString.Load();

            RWMistEntry.LoadResources();
        }

        public static void LoadOverDriveBarResources()
        {
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_Left", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_Left.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_Left_HighLightMask", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_Left_HighLightMask.png"), false);

            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_Right", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_Right.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_Right_HighLightMask", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_Right_HighLightMask.png"), false);

            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_Frame", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_Frame.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_Frame_HighLightMask", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_Frame_HighLightMask.png"), false);

            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_PipHighLight", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_PipHighLight.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_OverDriveBar_BarHighLight", LoadTexFromPath("illustrations/ReactorUI/OverDrive/DMPS_OverDriveBar_BarHighLight.png"), false);
        }

        public static void LoadFeedBackBarResources()
        {
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_FeedBackBar_Base", LoadTexFromPath("illustrations/ReactorUI/FeedBack/DMPS_FeedBackBar_Base.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_FeedBackBar_Mask", LoadTexFromPath("illustrations/ReactorUI/FeedBack/DMPS_FeedBackBar_Mask.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_FeedBackBar_Light", LoadTexFromPath("illustrations/ReactorUI/FeedBack/DMPS_FeedBackBar_Light.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_FeedBackBar_Fan_Light", LoadTexFromPath("illustrations/ReactorUI/FeedBack/DMPS_FeedBackBar_Fan_Light.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_FeedBackBar_Bar", LoadTexFromPath("illustrations/ReactorUI/FeedBack/DMPS_FeedBackBar_Bar.png"), false);
        }

        public static void LoadThunderBoltBarResources()
        {
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_BarLeft", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_BarLeft.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_BarRight", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_BarRight.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_PingAnvil", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_PingAnvil.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_PingHammer", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_PingHammer.png"), false);

            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_BarLeft_HL", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_BarLeft_HL.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_BarRight_HL", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_BarRight_HL.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_PingAnvil_HL", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_PingAnvil_HL.png"), false);
            Futile.atlasManager.LoadAtlasFromTexture("DMPS_ThunderBolt_PingHammer_HL", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_ThunderBolt_PingHammer_HL.png"), false);

            Futile.atlasManager.LoadAtlasFromTexture("DMPS_TunderBolt_SmashLight", LoadTexFromPath("illustrations/ReactorUI/ThunderBolt/DMPS_TunderBolt_SmashLight.png"), false);

        }

        public static void LoadShockEffectResources()
        {
            //DMPS_ShockEffect_0
            for(int i = 0;i < 4; i++)
            {
                Futile.atlasManager.LoadAtlasFromTexture($"DMPS_ShockEffect_{i}", LoadTexFromPath($"illustrations/ShockEffect/DMPS_ShockEffect_{i}.png"), false);
                Futile.atlasManager.LoadAtlasFromTexture($"DMPS_ShockEffect_Blr_{i}", LoadTexFromPath($"illustrations/ShockEffect/DMPS_ShockEffect_Blr_{i}.png"), false);
            }
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
