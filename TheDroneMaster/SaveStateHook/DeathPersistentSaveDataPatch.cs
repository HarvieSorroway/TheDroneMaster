using CustomSaveTx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;


namespace TheDroneMaster
{
    public class ScannedCreatureSaveUnit : DeathPersistentSaveDataTx
    {
        public override string header => "SCANNEDCREATURETYPES";

        public List<CreatureTemplate.Type> scanedTypes = new List<CreatureTemplate.Type>();

        public bool KingScanned => IsThisTypeScanned(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.ScavengerKing);
        public ScannedCreatureSaveUnit(SlugcatStats.Name name) : base(name)
        {
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            string[] allData = Regex.Split(data, "_");
            for(int i = 0;i < allData.Length; i++)
            {
                if (allData[i] == string.Empty)
                    continue;
                AddScannedType(new CreatureTemplate.Type(allData[i]));
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

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            scanedTypes.Clear();
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
                Plugin.Log("Add new creature type : " + type.ToString());
            }
        }
    }

    public class EnemyCreatorSaveUnit : DeathPersistentSaveDataTx
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

    public class SSConversationStateSaveUnit : DeathPersistentSaveDataTx
    {
        public override string header => "SSCONVERSATIONSTATE";
        public bool explainPackage = false;

        public SSConversationStateSaveUnit(SlugcatStats.Name name) : base(name)
        {
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            explainPackage = false;
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied || saveAsIfPlayerQuit) return origSaveData;
            return explainPackage.ToString();
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);
            if (data == string.Empty)
                return;
            explainPackage = bool.Parse(data);
        }
    }
}
