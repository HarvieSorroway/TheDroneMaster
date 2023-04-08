using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Menu.Remix.MixedUI;


namespace TheDroneMaster
{
    public class DroneMasterConfig : OptionInterface
    {
        public readonly Plugin instance;
        public readonly ManualLogSource logSource;

        public Configurable<bool> UsingHUDEffect;
        public Configurable<KeyCode> OpenHUDKey;
        public Configurable<bool> UsingPlayerInput;

        public Configurable<float> DamagePerShot;
        public Configurable<int> ChargeRequiresCounter;
        public Configurable<bool> KillTagFromPlayer;
        public Configurable<bool> HateCicadas;
        public Configurable<bool> Invincible;
        public Configurable<bool> OverPowerdSuperJump;

        public Configurable<bool> moreEnemies;
        public Configurable<bool> canBackSpear;

        UIelement[] uiElements;
        UIelement[] droneSettingsElements;
        UIelement[] difficultySettingsElements;

        public DroneMasterConfig(Plugin instance, ManualLogSource loggerSource)
        {
            this.instance = instance;
            this.logSource = loggerSource;

            UsingHUDEffect = config.Bind<bool>("DroneMaster_UsingHUDEffect", true);
            OpenHUDKey = config.Bind<KeyCode>("DroneMaster_OpenHUDKey", KeyCode.C);
            UsingPlayerInput = config.Bind<bool>("DroneMaster_UsingMouseInput", false);

            DamagePerShot = config.Bind<float>("DroneMaster_Drone_DamagePerShot", 1.15f);
            ChargeRequiresCounter = config.Bind<int>("DroneMaster_Drone_ChargeRequiresCounter", 80);
            KillTagFromPlayer = config.Bind<bool>("DroneMaster_Drone_KillTagFromPlayer", true);
            HateCicadas = config.Bind<bool>("DroneMaster_Drone_HateCicades", true);
            Invincible = config.Bind<bool>("DroneMaster_Drone_Invincible", false);
            OverPowerdSuperJump = config.Bind<bool>("DroneMaster_Drone_OverPowerdSuperJump", false);

            moreEnemies = config.Bind<bool>("DroneMaster_Difficulty_MoreEnemies", false);
            canBackSpear = config.Bind <bool> ("DroneMaster_Difficulty_CanBackSpear", true);
        }

        public override void Initialize()
        {
            base.Initialize();

            OpTab opTab = new OpTab(this, "Options");
            Tabs = new OpTab[]
            {
                opTab
            };

            float biasY = 30f;
            droneSettingsElements = new UIelement[]
            {
                new OpLabel(30f,300f - 10f - biasY,"Drone Options",true),
                new OpFloatSlider(DamagePerShot,new Vector2(30f,300f - 40f - biasY), 100)
                {
                    min = 0.1f,
                    max = 5f
                },
                new OpLabel(160f,300f - 40f - biasY,"How much damage can drone cause per shot", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpSlider(ChargeRequiresCounter,new Vector2(30f,300f-70f - biasY),100)
                {
                   min = 5,
                   max = 200
                },
                new OpLabel(160f,300f - 70f - biasY,"How many frames does drone need to charge gun", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpCheckBox(KillTagFromPlayer,30f,300f-100f - biasY),
                new OpLabel(160f,300f - 100f - biasY,"Whether the creatures killed by the drone are counted as player kills", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpCheckBox(HateCicadas,30f,300f-130f - biasY),
                new OpLabel(160f,300f - 130f - biasY,"If you hate cicadas,enable this to make drones kill them :D(cuz i really hate them :P)", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpCheckBox(Invincible,30f,300f-160f - biasY),
                new OpLabel(160f,300f - 160f - biasY,"Make drones invincible", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpCheckBox(OverPowerdSuperJump,30f,300f-190f - biasY),
                new OpLabel(160f,300f - 190f - biasY,"Make you jump much higher and run faster when grab a drone", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },

            };

            difficultySettingsElements = new UIelement[]
            {
                new OpLabel(30f,300f - 10f - biasY,"Difficulty Options",true),
                new OpCheckBox(moreEnemies,30f,300f - 40f - biasY),
                new OpLabelLong(new Vector2(160f,300f - 40f - biasY - 20f),new Vector2(80f,50f),"Spawn more and more dangerous creatures (beta function)\n   It only takes effect the first time the region is loaded,\n   and the save needs to be reset to cancel the effect", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpCheckBox(canBackSpear,30f,300f - 90f - biasY),
                new OpLabel(160f,300f - 90f - biasY,"Can DroneMaster back spear", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
            };


            var droneSettings = new OpScrollBox(new Vector2(10f, 300f - 30f), new Vector2(580f, 100f), 300f);
            var difficultySettings = new OpScrollBox(new Vector2(10, 170f - 30f), new Vector2(580f, 100f), 300f);

            this.uiElements = new UIelement[]
            {
                new OpLabel(10f, 550f, "Options", true),
                new OpCheckBox(UsingHUDEffect, 10f, 520f),
                new OpLabel(40f, 520f, "Enable DroneHUD effect (default to False if your computer doesn't support this)", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpKeyBinder(OpenHUDKey, new Vector2(10f, 480f), new Vector2(50f, 10f), true, OpKeyBinder.BindController.AnyController),
                new OpLabel(80f, 480f, "Key used for opening DroneHUD", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpCheckBox(UsingPlayerInput,10f,440f),
                new OpLabelLong(new Vector2(40f,440f - 20f),new Vector2(100f,50f),"Use PlayerInput to control DroneHUD instead of default mouse input\n(left/right/up/down replace mouse movement,throw replace mouse button down)", false)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                droneSettings,
                difficultySettings
            };
            opTab.AddItems(uiElements);

            droneSettings.AddItems(droneSettingsElements);
            difficultySettings.AddItems(difficultySettingsElements);
        }
    }
}
