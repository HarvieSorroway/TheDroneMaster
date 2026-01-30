using DMPS.PlayerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS
{
    internal static class Save
    {
        public static ConditionalWeakTable<SaveState, DMPSSaveState> saveStateMapper = new ConditionalWeakTable<SaveState, DMPSSaveState>();

        public static void HooksOn()
        {
            On.SaveState.ctor += SaveState_ctor;
            On.SaveState.SaveToString += SaveState_SaveToString;
            On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.SaveState.LoadGame += SaveState_LoadGame;
        }

        private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
        {
            orig.Invoke(self, str, game);
            if(TryGetDMPSSave(self, out var save, addIfMissing: false))
            {
                save.LoadGame(self, game);
            }
        }

        private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            if (TryGetDMPSSave(self, out var save, addIfMissing: false))
            {
                if (PlayerPatchs.TryGetModule<DMPSModule>(game.session.Players[0].realizedCreature as Player, out var module))
                {
                    save.MaxEnergy = Mathf.Min(100, save.MaxEnergy + 10);
                    save.Energy = module.bioReactor.reactorEnergy;
                }
            }
            orig.Invoke(self, game, survived, newMalnourished);
        }

        private static string SaveState_SaveToString(On.SaveState.orig_SaveToString orig, SaveState self)
        {
            string res = orig.Invoke(self);
            if(TryGetDMPSSave(self, out var save, addIfMissing: false))
                res += save.SaveToString();
            return res;
        }

        private static void SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig.Invoke(self, saveStateNumber, progression);
            if(saveStateNumber == DMEnums.DMPS.SlugStateName.DMPS)
                saveStateMapper.Add(self, new DMPSSaveState());
        }

        public static bool TryGetDMPSSave(SaveState saveState, out DMPSSaveState save, bool addIfMissing = true, RainWorldGame game = null)
        {
            bool res = saveStateMapper.TryGetValue(saveState, out save);
            if(!res && addIfMissing)
            {
                save = new DMPSSaveState();
                saveStateMapper.Add(saveState, save);
                save.LoadGame(saveState, game);
            }
            return res;
        }
    }

    internal class DMPSSaveState
    {
        const string DMPSSaveHeader = "DMPSSAVE";
        public int MaxEnergy { get; set; }//默认30最大
        public float Energy { get; set; }

        public DMPSSaveState()
        {
            MaxEnergy = 30;
            Energy = 30;
        }

        public void LoadGame(SaveState saveState, RainWorldGame game)
        {
            List<string[]> list = new List<string[]>();
            for (int i = 0; i < saveState.unrecognizedSaveStrings.Count; i++)
            {
                string[] array2 = Regex.Split(saveState.unrecognizedSaveStrings[i], "<svB>");
                if (array2.Length != 0 && array2[0].Length > 0)
                {
                    list.Add(array2);
                }
            }

            for (int i = 0;i < list.Count;i++)
            {
                if (list[i][0] == "DMPSSaveHeader")
                {
                    var items = Regex.Split(list[i][1], "<svC>");

                    if(items.Length > 0) MaxEnergy = int.Parse(items[0]);
                    if(items.Length > 1) Energy = float.Parse(items[1]);

                    Plugin.Log($"DMPSSaveState load from:{saveState.unrecognizedSaveStrings[i]}\n   MaxEnergy:{MaxEnergy}\n   Energy{Energy}");
                    saveState.unrecognizedSaveStrings.RemoveAt(i);
                    break;
                }
            }
           
        }

        public string SaveToString()
        {
            string res = $"DMPSSaveHeader<svB>{MaxEnergy}<svC>{Energy}<svA>";
            Plugin.Log($"DMPSSaveState save:{res}");
            return res;
        }
    }
}
