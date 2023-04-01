using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;


namespace TheDroneMaster
{
    public static class DeathPersistentSaveDataPatch
    {
        public static string TotalHeader => Plugin.ID.ToUpper();

        public static bool UnitsLoaded = false;
        public static List<DeathPersistentSaveDataUnit> units = new List<DeathPersistentSaveDataUnit>();
        
        public static void Patch()//call Patch() in OnModInit
        {
            On.SaveState.ctor += SaveState_ctor;

            On.DeathPersistentSaveData.FromString += DeathPersistentSaveData_FromString;
            On.DeathPersistentSaveData.SaveToString += DeathPersistentSaveData_SaveToString;
        }

        private static void SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig.Invoke(self, saveStateNumber, progression);
            if (UnitsLoaded)
            {
                foreach(var unit in units)
                {
                    unit.ClearDataForNewSaveState(saveStateNumber);
                }
            }
            else//Load DeathPersistentSaveDataUnits here
            {
                units.Add(new EnemyCreatorSaveUnit(saveStateNumber));
                UnitsLoaded = true;
            }
        }

        private static string DeathPersistentSaveData_SaveToString(On.DeathPersistentSaveData.orig_SaveToString orig, DeathPersistentSaveData self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            string result = orig.Invoke(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);

            foreach(var unit in units)
            {
                string header = unit.header;
                string data = unit.SaveToString(saveAsIfPlayerDied, saveAsIfPlayerQuit);
                if(header != string.Empty && data != string.Empty)
                {
                    result += TotalHeader + header + "<dpB>" + data + "<dpA>";
                }
                Plugin.Log(header + " Save to string : " + data);
            }
            return result;
        }

        static private void DeathPersistentSaveData_FromString(On.DeathPersistentSaveData.orig_FromString orig, DeathPersistentSaveData self, string s)
        {
            orig.Invoke(self, s);

            string[] array = Regex.Split(s, "<dpA>");
            for (int i = 0; i < array.Length; i++)
            {
                string[] array2 = Regex.Split(array[i], "<dpB>");
                string header = array2[0];

                foreach(var unit in units)
                {
                    if (TotalHeader + unit.header == header)
                    {
                        for(int k = self.unrecognizedSaveStrings.Count - 1;k >= 0; k--)
                        {
                            if (self.unrecognizedSaveStrings[k].Contains(header)) self.unrecognizedSaveStrings.RemoveAt(k);
                        }
                        unit.LoadDatas(array2[1]);
                        Plugin.Log(unit.header + " load from string : " + array2[1]);
                    }
                }
            }
        }

        public static DeathPersistentSaveDataUnit GetUnitOfHeader(string header)
        {
            foreach(var unit in units)
            {
                if (unit.header == header) return unit;
            }
            return null;
        }
    }

    public class DeathPersistentSaveDataUnit
    {
        public SlugcatStats.Name slugName;

        public string origSaveData;
        public virtual string header => "";
  
        public DeathPersistentSaveDataUnit(SlugcatStats.Name name)
        {
            slugName = name;
        }

        public virtual string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            return "";
        }

        public virtual void LoadDatas(string data)
        {
            origSaveData = data;
        }

        public virtual void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            origSaveData = "";
            slugName = newSlugName;
        }

        public override string ToString()
        {
            return base.ToString() + " SlugStateName:" + slugName.ToString() + " header:" + header;
        }
    }

    //This is a simpleTest
    public class TestSaveUnit : DeathPersistentSaveDataUnit
    {
        public int loadThisForHowManyTimes = 0;
        public TestSaveUnit(SlugcatStats.Name name) : base(name)
        {
        }

        public override string header => "THISISJUSTATESTLOL";

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            loadThisForHowManyTimes = int.Parse(data);
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied || saveAsIfPlayerQuit) return origSaveData;
            else
            {
                loadThisForHowManyTimes++;
                return loadThisForHowManyTimes.ToString();
            }
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            loadThisForHowManyTimes = 0;
        }

        public override string ToString()
        {
            return base.ToString() + " loadThisForHowManyTimes:" + loadThisForHowManyTimes.ToString();
        }
    }

    public class ScannedCreatureSaveUnit : DeathPersistentSaveDataUnit
    {
        public override string header => "SCANNEDCREATURETYPES";

        public List<CreatureTemplate.Type> scanedTypes = new List<CreatureTemplate.Type>();
        public ScannedCreatureSaveUnit(SlugcatStats.Name name) : base(name)
        {
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            string[] allData = Regex.Split(data, "_");
            for(int i = 0;i < allData.Length; i++)
            {
                scanedTypes.Add(new CreatureTemplate.Type(allData[i]));
            }
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied | saveAsIfPlayerQuit) return origSaveData;
            if (scanedTypes.Count == 0) return "";

            string result = "";
            for(int i = 0;i < scanedTypes.Count; i++)
            {
                result += scanedTypes[i].value;
                result += "_";
            }

            return result;
        }

        public bool IsThisTypeScanned(CreatureTemplate.Type type)
        {
            return scanedTypes.Contains(type);
        }

        public void AddScannedType(CreatureTemplate.Type type)
        {
            if (!IsThisTypeScanned(type))
            {
                scanedTypes.Add(type);
            }
        }
    }

    public class EnemyCreatorSaveUnit : DeathPersistentSaveDataUnit
    {
        public override string header => "ENEMYCREATOR";

        public List<string> CreateEnemyOrNot = new List<string>();

        public EnemyCreatorSaveUnit(SlugcatStats.Name name) : base(name)
        {
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            CreateEnemyOrNot.Clear();
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);
            CreateEnemyOrNot.Clear();
            string[] regions = Regex.Split(data, "_");
            for(int i = 0;i < regions.Length; i++)
            {
                Plugin.Log(regions[i]);
                if (regions[i] == string.Empty || CreateEnemyOrNot.Contains(regions[i])) continue;
                CreateEnemyOrNot.Add(regions[i]);
            }
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied || saveAsIfPlayerQuit) return origSaveData;
            if (CreateEnemyOrNot.Count == 0) return "";
            string result = "";
            for(int i = 0;i < CreateEnemyOrNot.Count; i++)
            {
                result += CreateEnemyOrNot[i];
                result += "_";
            }
            Plugin.Log(result);
            return result;
        }

        public override string ToString()
        {
            return base.ToString() + " " + SaveToString(false,false);
        }

        public bool isThisRegionSpawnOrNot(Region region)
        {
            return CreateEnemyOrNot.Contains(region.name);
        }

        public void SpawnEnemyInNewRegion(Region region)
        {
            CreateEnemyOrNot.Add(region.name);
        }
    }
}
