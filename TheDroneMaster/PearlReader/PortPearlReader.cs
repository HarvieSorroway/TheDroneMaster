using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    
    public class PortPearlReader
    {
        public DronePort port;
        public List<string> pearlConvs = new List<string>();

        public static List<string> convIDNames;
        public static List<Conversation.ID> convIDs;

        public static bool setup = false;

        public static SLOracleBehaviorHasMark simulateSLOracleBehaviour;

        public static void Init()
        {
            if (setup) return;

            List<string> names = Conversation.ID.values.entries;
            List<Conversation.ID> idArray = new List<Conversation.ID>();
            foreach (var name in names)
            {
                idArray.Add(new Conversation.ID(name));
            }

            convIDNames = names;
            convIDs = idArray;
            setup = true;
            SetupBehavior();
        }

        private static void SetupBehavior()
        {
            try
            {
                simulateSLOracleBehaviour = GetUninit<SLOracleBehaviorHasMark>();
                simulateSLOracleBehaviour.oracle = GetUninit<Oracle>();
                simulateSLOracleBehaviour.oracle.ID = GetUninit<Oracle.OracleID>();
                simulateSLOracleBehaviour.oracle.room = GetUninit<Room>();
                simulateSLOracleBehaviour.oracle.room.game = GetUninit<RainWorldGame>();
                simulateSLOracleBehaviour.oracle.room.game.rainWorld = UnityEngine.Object.FindObjectOfType<RainWorld>();
                simulateSLOracleBehaviour.oracle.room.game.session = GetUninit<SandboxGameSession>();
                simulateSLOracleBehaviour.currentConversation = GetUninit<SLOracleBehaviorHasMark.MoonConversation>();
                simulateSLOracleBehaviour.currentConversation.dialogBox = GetUninit<HUD.DialogBox>();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public Conversation.ID FindConvo(DataPearl.AbstractDataPearl.DataPearlType type)
        {
            Init();
            string text = type.ToString();

            foreach (string str in convPrefix)
            {
                for (int j = 0; j < convIDNames.Count; j++)
                {
                    if (convIDNames[j] == str + text) return convIDs[j];
                }
            }
            for (int k = 0; k < convIDNames.Count; k++)
            {
                if (convIDNames[k].EndsWith(text)) return convIDs[k];
            }
            for (int l = 0; l < convIDNames.Count; l++)
            {
                if (convIDNames[l].Contains(text)) return convIDs[l];
            }

            Debug.LogException(new Exception(string.Format("Failed to get conversation for pearl: {0}", type)));
            return convIDs[0];
        }

        public Conversation.DialogueEvent[] GetEvent(Conversation.ID id, int index = -1)
        {
            PearlReaderPatchs.SkipIntro = true;
            Conversation.DialogueEvent[] result =  new SLOracleBehaviorHasMark.MoonConversation(id, simulateSLOracleBehaviour, SLOracleBehaviorHasMark.MiscItemType.NA).events.ToArray();
            PearlReaderPatchs.SkipIntro = false;

            return result;
        }

        public static T GetUninit<T>()
        {
            return (T)(FormatterServices.GetSafeUninitializedObject(typeof(T)));
        }


        private static string[] convPrefix = new string[]
        {
            "Moon_Pearl",
            "Moon_Pearl_",
            "Moon_",
            "Pearl_",
            ""
        };
    }
}
