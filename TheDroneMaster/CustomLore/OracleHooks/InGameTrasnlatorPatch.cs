using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public static class InGameTrasnlatorPatch
    {
        static Dictionary<string, string> shortStrings = new Dictionary<string, string>();
        public static void Patch()
        {
            On.InGameTranslator.LoadShortStrings += InGameTranslator_LoadShortStrings;
        }

        public static void LoadResource()
        {
            string[] origs = Regex.Split(DroneMasterRes.Origs,"\n");
            string[] trans = Regex.Split(DroneMasterRes.Translations, "\n");

            for(int i = 0;i < origs.Length; i++)
            {
                shortStrings.Add(origs[i].Trim(), trans[i].Trim());
            }
        }

        private static void InGameTranslator_LoadShortStrings(On.InGameTranslator.orig_LoadShortStrings orig, InGameTranslator self)
        {
            orig.Invoke(self);

            if (self.currentLanguage != InGameTranslator.LanguageID.Chinese) return;

            foreach(var pair in shortStrings)
            {
                if(self.shortStrings.ContainsKey(pair.Key)) 
                    continue;
                self.shortStrings.Add(pair.Key, pair.Value);
                Plugin.Log("Load Trans : " + pair.Key + " - " + pair.Value);
            }
        }
    }
}
