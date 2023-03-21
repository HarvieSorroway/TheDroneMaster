using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx;
using System.Security.Permissions;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using RWCustom;
using Fisobs.Core;
using SlugBase.DataTypes;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.IO;


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
        public static readonly string ID = "thedronemaster";

        public static readonly bool LogOutPut = true;

        public static Plugin instance;

        public static Shader postShade;
        public static Shader bufferShader;
        public static Shader customHoloGridShader;
        public static PostEffect postEffect;

        public static bool inited;
        public static OnMouseButtonDown onMouseButtonDown;
        public static OnMouseButtonUp onMouseButtonUp;
        public static FrameUpdate frameUpdate;

        public DroneMasterConfig config;

        public Dictionary<Player, List<LaserDrone>> dronesForPlayer = new Dictionary<Player, List<LaserDrone>>();

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

            PlayerPatchs.Patch();
            PlayerGraphicsPatch.Patch();
            HUDPatch.Patch();
            CreaturePatchs.Patch();
            GatePatchs.Patch();
            SSOracleActionPatch.Patch();
            InGameTrasnlatorPatch.Patch();
            Fixer.Patch();
            DeathPersistentSaveDataPatch.Patch();
            //PearlReaderPatchs.Patch();

            DroneMasterEnums.RegisterValues();

            LoadResources(self);

            MachineConnector.SetRegisteredOI("harvie.thedronemaster", config);
            inited = true;
        }

        public void LoadResources(RainWorld rainWorld)
        {
            InGameTrasnlatorPatch.LoadResource();

            string path = AssetManager.ResolveFilePath("assetbundles/posttestshader");
            AssetBundle ab = AssetBundle.LoadFromFile(path);

            postShade = ab.LoadAsset<Shader>("assets/posttestshader.shader");
            bufferShader = ab.LoadAsset<Shader>("assets/buffershader.shader");
            customHoloGridShader = ab.LoadAsset<Shader>("assets/customhologrid.shader");

            Camera cam = GameObject.FindObjectOfType<Camera>();
            postEffect = cam.gameObject.AddComponent<PostEffect>();

            rainWorld.Shaders.Add("CustomHoloGrid", FShader.CreateShader("CustomHoloGrid", customHoloGridShader));

            ab.Unload(false);
        }

        public delegate void OnMouseButtonDown(int button);
        public delegate void OnMouseButtonUp(int button);
        public delegate void FrameUpdate();

        public static void Log(string text)
        {
            if (!LogOutPut) return;
            Debug.Log("[DroneMaster]" + text);
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

        public static void RegisterValues()
        {
            if (registed) return;
            MeetDroneMaster = new SSOracleBehavior.Action("MeetDroneMaster", true);
            Meet_DroneMaster = new SSOracleBehavior.SubBehavior.SubBehavID("Meet_DroneMaster", true);

            Pebbles_DroneMaster_FirstMeet = new Conversation.ID("Pebbles_DroneMaster_FirstMeet", true);
            Pebbles_DroneMaster_AfterMet = new Conversation.ID("Pebbles_DroneMaster_AfterMet", true);
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
                registed = false;
            }
        }
    }
}
