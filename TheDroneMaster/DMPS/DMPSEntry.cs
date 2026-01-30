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
using TheDroneMaster.DMPS.DMPShud;
using TheDroneMaster.DMPS.DMPSSave;
using TheDroneMaster.DMPS.DMPSSkillTree;
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
            MenuHooks.MenuHooks.HooksOn();
            //Save.HooksOn();
            RenderNodeLoader.Load();
            SkillNodeLoader.Load();
            SkillTreeHooks.HooksOn();

            DeathPersistentSaveDataRx.AppplyTreatment(new DMPSBasicSave(null));
            
            //DMPSSkillTree.SkillTreeRenderNode nodeTest = new DMPSSkillTree.SkillTreeRenderNode();
            //nodeTest.bindSkillNodeInfo = "SkillTree.TestSkill1";
            //nodeTest.renderNodeIDInfo = "RenderNode.TestSkill1";
            //nodeTest.scaleInfo = 1.5f;
            //nodeTest.typeInfo = DMPSSkillTree.SkillTreeRenderType.StaticNode;
            //nodeTest.posInfo = new Vector2[3] { Vector2.zero, new Vector2(10f, 0f), new Vector2(10f, -5f) };
            //nodeTest.subRenderNodeInfo = new string[] { "RenderNode.DronePortUpg", "RenderNode.PackUpg", "RenderNode.ReactorUpg" };

            //DMPSSkillTree.SkillTreeRenderNode nodeTest2 = new DMPSSkillTree.SkillTreeRenderNode();
            //nodeTest2.bindSkillNodeInfo = "SkillTree.DronePortUpg";
            //nodeTest2.renderNodeIDInfo = "RenderNode.DronePortUpg";
            //nodeTest2.scaleInfo = 1;
            //nodeTest2.typeInfo = DMPSSkillTree.SkillTreeRenderType.StaticNode;
            //nodeTest2.posInfo = new Vector2[] {  new Vector2(10f, -5f) };
            //nodeTest2.subRenderNodeInfo = new string[] { "RenderNode.DronePortUpg.CoolDown0" };

            //SkillNode skillNodeTest = new SkillNode();
            //skillNodeTest.skillID = "Skill.DronePortUpg.CoolDown1";
            //skillNodeTest.cost = 10f;
            //skillNodeTest.descriptionInfos = new Dictionary<string, SkillNodeDescriptionInfo>()
            //{
            //   { InGameTranslator.LanguageID.English.value, new SkillNodeDescriptionInfo()
            //       {
            //           name = "Drone produce cd upgrade lv.1",
            //           description = "Wawa description test."
            //       } 
            //   },
            //   { InGameTranslator.LanguageID.Chinese.value, new SkillNodeDescriptionInfo()
            //       {
            //           name = "无人机召唤cd lv.1",
            //           description = "wawa这是一个测试"
            //       }
            //   }
            //};
            //skillNodeTest.conditions = new List<SKillNodeConditionInfo>()
            //{
            //    new SKillNodeConditionInfo()
            //    {
            //        type = ConditionType.SkillNode,
            //        boolType = ConditionBoolType.And,
            //        info = "Skill.DronePortUpg.CoolDown0"
            //    }
            //};
            //Plugin.LoggerLog(DMPSSkillTreeHelper.SerializeSkillNode(skillNodeTest));


            //SkillNodeLoader.Load();
            //foreach (var id in SkillNodeLoader.loadedSkillNodes.Keys)
            //{
            //    Plugin.LoggerLog(DMPSSkillTreeHelper.SerializeSkillNode(SkillNodeLoader.loadedSkillNodes[id]));

            //}

            //Plugin.LoggerLog(DMPSSkillTreeHelper.SerializeRenderNodes(new List<DMPSSkillTree.SkillTreeRenderNode>() { nodeTest, nodeTest2}));
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

            DMPSResourceString.Load();
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
