using BepInEx;
using Fisobs.Core;
using SlugBase.DataTypes;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using TheDroneMaster.DreamComponent.OracleHooks;
using TheDroneMaster.GameHooks;
using UnityEngine;
using static SlugBase.Features.FeatureTypes;
using RWCustom;
using Fisobs.Core;
using SlugBase.DataTypes;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.IO;
using TheDroneMaster.GameHooks;
using TheDroneMaster.DreamComponent;
using TheDroneMaster.DreamComponent.OracleHooks;
using TheDroneMaster.DreamComponent.DreamHook;
using TheDroneMaster.CustomLore.SpecificScripts;
using Menu;
using TheDroneMaster.CustomLore.CustomEnding;
using TheDroneMaster.CreatureAndObjectHooks;
using static Menu.SlideShow;


#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace TheDroneMaster
{
    [BepInPlugin("harvie.thedronemaster", "TheDroneMaster", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly PlayerFeature<bool> OwnLaserDrone = PlayerBool("TheDroneMaster/own_laserdrone");
        public static readonly PlayerFeature<PlayerColor> portColor = PlayerCustomColor("Port");
        public static readonly string DroneMasterName = "thedronemaster";

        public static readonly bool LogOutPut = true;

        public static Plugin instance;

        public static Shader postShade;
        public static Shader bufferShader;
        public static Shader customHoloGridShader;
        public static Shader lineMaskShader;
        public static Shader dataWaveShader;
        public static PostEffect postEffect;

        public static bool inited;
        public static OnMouseButtonDown onMouseButtonDown;
        public static OnMouseButtonUp onMouseButtonUp;
        public static FrameUpdate frameUpdate;

        public static string falseRectName;
        public static string trueRectName;

        public DroneMasterConfig config;

        public Dictionary<Player, List<LaserDrone>> dronesForPlayer = new Dictionary<Player, List<LaserDrone>>();

        public static int down = 0;
        public static int subStract = 0;
        //hook

        public Plugin()
        {
            try
            {
                config = new DroneMasterConfig(this, Logger);
                instance = this;
            }
            catch(Exception e)
            {
                Logger.LogError(e);
            }
        }
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            Content.Register(new LaserDroneCritob());
            //DreamSessionHook.RegisterDream(new DroneMasterDream());
        }

        public void Start()
        {
            
        }

        public void Update()
        {
            if (frameUpdate != null) frameUpdate();
            for (int i = 0; i < 2; i++)
            {
                if (Input.GetMouseButtonDown(i) && onMouseButtonDown != null) onMouseButtonDown(i);
                if (Input.GetMouseButtonUp(i) && onMouseButtonUp != null) onMouseButtonUp(i);
            }
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (inited) return;

            try
            {
                PlayerPatchs.Patch();
                PlayerGraphicsPatch.Patch();
                HUDPatch.Patch();
                CreaturePatchs.Patch();
                GatePatchs.Patch();
                SSOracleActionPatch.Patch();
                InGameTrasnlatorPatch.Patch();
                Fixer.Patch();
                DeathPersistentSaveDataPatch.Patch();
                GamePatch.Patch(self);
                SessionHook.Patch();
                RoomSpecificScriptPatch.PatchOn();
                CustomEnding.PatchOn();
                ObjectPatch.PatchOn();
                //PearlReaderPatchs.Patch();

                //TODO: DEBUG
                On.Player.Update += Player_Update;

                OraclePatch.PatchOn();

                DroneMasterEnums.RegisterValues();

                LoadResources(self);

                MachineConnector.SetRegisteredOI("harvie.thedronemaster", config);
                inited = true;
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }


        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self,eu);
            if (Input.GetKey(KeyCode.Y))
            {
                foreach (var i in self.room.drawableObjects)
                {
                    if (i is DroneMasterEnding)
                    {
                        return;
                    }
                }
                self.room.AddObject(new DroneMasterEnding(self.room));
                //if (PlayerPatchs.modules.TryGetValue(self,out var module))
                //    a.module= module;
            }
            else if (Input.GetKey(KeyCode.N))
            {
                DroneMasterEnding a = null;
                foreach (var i in self.room.drawableObjects)
                {
                    if (i is DroneMasterEnding)
                    {
                        a = i as DroneMasterEnding;
                    }
                    if (i is DroneMasterEnding.EndingMessageSender)
                    {
                        return;
                    }
                }
                if(a!= null)
                self.room.AddObject(new DroneMasterEnding.EndingMessageSender(a));
                //if (PlayerPatchs.modules.TryGetValue(self,out var module))
                //    a.module= module;
            }

        }

        public void LoadResources(RainWorld rainWorld)
        {
            InGameTrasnlatorPatch.LoadResource();

            string path = AssetManager.ResolveFilePath("assetbundles/posttestshader");
            AssetBundle ab = AssetBundle.LoadFromFile(path);

            postShade = ab.LoadAsset<Shader>("assets/posttestshader.shader");
            bufferShader = ab.LoadAsset<Shader>("assets/buffershader.shader");
            customHoloGridShader = ab.LoadAsset<Shader>("assets/customhologrid.shader");
            dataWaveShader = ab.LoadAsset<Shader>("assets/myshader/datawave.shader");

            Camera cam = GameObject.FindObjectOfType<Camera>();
            postEffect = cam.gameObject.AddComponent<PostEffect>();

            rainWorld.Shaders.Add("CustomHoloGrid", FShader.CreateShader("CustomHoloGrid", customHoloGridShader));
            rainWorld.Shaders.Add("DataWave", FShader.CreateShader("DataWave", dataWaveShader));

            FAtlas falseRect = Futile.atlasManager.LoadImage("assetbundles/SelectRectFalse");
            falseRectName = falseRect.name;

            FAtlas trueRect = Futile.atlasManager.LoadImage("assetbundles/SelectRectTrue");
            trueRectName = trueRect.name;

            ab.Unload(false);

            string path2 = AssetManager.ResolveFilePath("assetbundles/linemaskshader");
            AssetBundle ab2 = AssetBundle.LoadFromFile(path2);
            lineMaskShader = ab2.LoadAsset<Shader>("assets/LineMask.shader");
            rainWorld.Shaders.Add("LineMask", FShader.CreateShader("LineMask", lineMaskShader));

        }

        public delegate void OnMouseButtonDown(int button);
        public delegate void OnMouseButtonUp(int button);
        public delegate void FrameUpdate();

        public static void Log(string text)
        {
            if (!LogOutPut) return;
            Debug.Log("[DroneMaster]" + text);
        }

        public static void LoggerLog(string text)
        {
            instance.Logger.LogDebug(text);
        }

        public static void Log(string pattern, params object[] objects)
        {
            if(!LogOutPut) return;
            Debug.Log("[DroneMaster]" + string.Format(pattern, objects));
        }
    }

    public class DroneMasterEnums
    {
        public static bool registed = false;
        public static SSOracleBehavior.Action MeetDroneMaster;
        public static SSOracleBehavior.SubBehavior.SubBehavID Meet_DroneMaster;

        //Conversation.ID
        public static Conversation.ID Pebbles_DroneMaster_FirstMeet;
        public static Conversation.ID Pebbles_DroneMaster_AfterMet;

        public static Conversation.ID Pebbles_DroneMaster_ExplainPackage;

        //Ending
        public static SlideShow.SlideShowID DroneMasterAltEnd;
        public static SlideShow.SlideShowID DroneMasterIntro;

        //Scene
        public static MenuScene.SceneID TheDroneMaster_Outro1;
        public static MenuScene.SceneID TheDroneMaster_Outro2;
        public static MenuScene.SceneID TheDroneMaster_Outro3;
        public static MenuScene.SceneID TheDroneMaster_AltEndScene;

        //Sound
        public static SoundID DataHumming;

        public static void RegisterValues()
        {
            if (registed) return;
            MeetDroneMaster = new SSOracleBehavior.Action("MeetDroneMaster", true);
            Meet_DroneMaster = new SSOracleBehavior.SubBehavior.SubBehavID("Meet_DroneMaster", true);

            Pebbles_DroneMaster_FirstMeet = new Conversation.ID("Pebbles_DroneMaster_FirstMeet", true);
            Pebbles_DroneMaster_AfterMet = new Conversation.ID("Pebbles_DroneMaster_AfterMet", true);

            Pebbles_DroneMaster_ExplainPackage = new Conversation.ID("Pebbles_DroneMaster_ExplainPackage", true);

            DroneMasterAltEnd = new SlideShow.SlideShowID("DroneMasterAltEnd", true);
            DroneMasterIntro = new SlideShow.SlideShowID("DroneMasterIntro", true);

            TheDroneMaster_Outro1 = new MenuScene.SceneID("TheDroneMaster_Outro1", true);
            TheDroneMaster_Outro2 = new MenuScene.SceneID("TheDroneMaster_Outro2", true);
            TheDroneMaster_Outro3 = new MenuScene.SceneID("TheDroneMaster_Outro3", true);
            TheDroneMaster_AltEndScene = new MenuScene.SceneID("TheDroneMaster_AltEndScene", true);

            DataHumming = new SoundID("Data_Humming", true);
            registed = true;
        }

        public static void UnregisterValues()
        {
            if(registed)
            {
                MeetDroneMaster.Unregister();
                MeetDroneMaster = null;

                Meet_DroneMaster.Unregister();
                Meet_DroneMaster = null;

                Pebbles_DroneMaster_FirstMeet.Unregister();
                Pebbles_DroneMaster_FirstMeet = null;

                Pebbles_DroneMaster_AfterMet.Unregister();
                Pebbles_DroneMaster_AfterMet = null;

                Pebbles_DroneMaster_ExplainPackage.Unregister();
                Pebbles_DroneMaster_ExplainPackage = null;

                DroneMasterAltEnd.Unregister();
                DroneMasterAltEnd = null;

                DroneMasterIntro.Unregister();
                DroneMasterIntro = null;
                registed = false;
            }
        }
    }
}
