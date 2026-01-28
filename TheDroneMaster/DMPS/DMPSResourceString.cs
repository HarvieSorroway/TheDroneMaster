using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS
{
    internal static class DMPSResourceString
    {
        static Dictionary<InGameTranslator.LanguageID, Dictionary<string, string>> resourceStr = new Dictionary<InGameTranslator.LanguageID, Dictionary<string, string>>();
        
        public static void Load()
        {
            string pathOrig = AssetManager.ResolveDirectory("resourcestring");
            foreach(var lang in InGameTranslator.LanguageID.values.entries)
            {
                var path = Path.Combine(pathOrig, lang.ToString() + ".txt");
                if (File.Exists(path))
                {
                    var dict = new Dictionary<string, string>();
                    resourceStr.Add(new InGameTranslator.LanguageID(lang), dict);
                    var lines = File.ReadAllLines(path);

                    foreach(var l in lines)
                    {
                        if (!string.IsNullOrEmpty(l) && !l.StartsWith("#"))
                        {
                            var s = l.Split('|');
                            Plugin.LoggerLog($"Load Resource String : {lang} : {s[0].Trim()} | {s[1].Trim()}");
                            dict.Add(s[0].Trim(), s[1].Trim());
                        }
                    }
                }
            }
        }
        
        public static string Get(string key)
        {
            return Get(key, Custom.rainWorld.inGameTranslator.currentLanguage);
        }

        public static string Get(string key, InGameTranslator.LanguageID lang)
        {
            if (resourceStr.TryGetValue(lang, out var dict))
            {
                if (dict.TryGetValue(key, out var val))
                    return val;
                else
                    return $"ERR KEY : {key}";
            }
            else
                return Get(key, InGameTranslator.LanguageID.English);
        }
    }
}
