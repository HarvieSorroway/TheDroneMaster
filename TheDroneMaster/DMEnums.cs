using JetBrains.Annotations;
using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster
{
    internal static class DMEnums
    {
        public static SSOracleBehavior.Action MeetDroneMaster = new SSOracleBehavior.Action("MeetDroneMaster", true);
        public static SSOracleBehavior.SubBehavior.SubBehavID Meet_DroneMaster = new SSOracleBehavior.SubBehavior.SubBehavID("Meet_DroneMaster", true);

        //Conversation.ID
        public static Conversation.ID Pebbles_DroneMaster_FirstMeet = new Conversation.ID("Pebbles_DroneMaster_FirstMeet", true);
        public static Conversation.ID Pebbles_DroneMaster_AfterMet = new Conversation.ID("Pebbles_DroneMaster_AfterMet", true);

        public static Conversation.ID Pebbles_DroneMaster_ExplainPackage = new Conversation.ID("Pebbles_DroneMaster_ExplainPackage", true);
        public static Conversation.ID Pebbles_DroneMaster_ExplainPackageFirstMeet = new Conversation.ID("Pebbles_DroneMaster_ExplainPackageFirstMeet", true);

        //Ending
        public static SlideShow.SlideShowID DroneMasterAltEnd = new SlideShow.SlideShowID("DroneMasterAltEnd", true);
        public static SlideShow.SlideShowID DroneMasterIntro = new SlideShow.SlideShowID("DroneMasterIntro", true);

        //Scene
        public static MenuScene.SceneID TheDroneMaster_Outro1 = new MenuScene.SceneID("TheDroneMaster_Outro1", true);
        public static MenuScene.SceneID TheDroneMaster_Outro2 = new MenuScene.SceneID("TheDroneMaster_Outro2", true);
        public static MenuScene.SceneID TheDroneMaster_Outro3 = new MenuScene.SceneID("TheDroneMaster_Outro3", true);
        public static MenuScene.SceneID TheDroneMaster_AltEndScene = new MenuScene.SceneID("TheDroneMaster_AltEndScene", true);

        //Sound
        public static SoundID DataHumming;
        public static SoundID DataWaveShock;

        public static class SlugStateName
        {
            public static SlugcatStats.Name DroneMaster = new SlugcatStats.Name("thedronemaster");
        }

        public static void RegisterValues()
        {
            DataHumming = new SoundID("Data_Humming", true);
            DataWaveShock = new SoundID("DataWaveShock", true);

            DMPS.Sound.JetJump = new SoundID("DMPS_JetJump", true);
            DMPS.Sound.DMPS_DroneAttack_Default = new SoundID("DMPS_DroneAttack_Default", true);
            DMPS.Sound.DMPS_DroneMoveLoop = new SoundID("DMPS_DroneMoveLoop", true);
        }

        public static class DMPS
        {
            public static class PlayerAnimationIndex
            {
                public static Player.AnimationIndex DMFlip = new Player.AnimationIndex("DMFlip", true);
            }

            public static class CreatureTemplateType
            {
                public static CreatureTemplate.Type DMPSDrone = new CreatureTemplate.Type("DMPSDrone", true);
            }

            public static class SlugStateName
            {
                public static SlugcatStats.Name DMPS = new SlugcatStats.Name("dmps");
            }
            public static class Sound
            {
                public static SoundID JetJump;
                public static SoundID DMPS_DroneAttack_Default;
                public static SoundID DMPS_DroneMoveLoop;
            }

        }
    }
}
