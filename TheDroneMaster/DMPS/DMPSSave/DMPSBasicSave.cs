using CustomSaveTx;
using EmgTx.CustomSaveTx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSkillTree;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSave
{
    internal partial class DMPSBasicSave : DeathPersistentSaveDataTx
    {
        public const string Header = "DMPSSKILLTREESAVE";
        public override string header => Header;

        readonly SaveUnit<float> energy;
        readonly SaveUnit<int> activeDroneCountSaveUnit;
        readonly SaveUnit<HashSet<string>> enabledSkills;

        public DMPSSkillData SkillData { get; } = new DMPSSkillData();

        public float Energy
        {
            get => energy.Value;
            set => energy.Value = Mathf.Clamp(value, 0, SkillData.MaxEnergy);
        }
        public int activeDroneCount
        {
            get => activeDroneCountSaveUnit.Value;
            set => activeDroneCountSaveUnit.Value = value;
        }

        public DMPSBasicSave(SlugcatStats.Name name) : base(name)
        {
            energy = AddSaveUnit("energy", 0f);
            activeDroneCountSaveUnit = AddSaveUnit("activeDroneCount", 0);
            enabledSkills = AddSaveUnit("enabledSkills", new HashSet<string>());
            UpdateSkillData();
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            UpdateSkillData();

            //Plugin.LoggerLog($"LoadDatas : Energy={Energy}, activeDroneCount={activeDroneCount}, enabledSkills={string.Join(",", enabledSkills.Value)}");
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            return string.Empty;
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            UpdateSkillData();
        }

        public bool CheckSkill(string id)
        {
            return enabledSkills.Value.Contains(id);
        }

        public HashSet<string> GetEnabledSkillsSnapshot()
        {
            return new HashSet<string>(enabledSkills.Value);
        }

        public void ReplaceSkillTreeState(IEnumerable<string> skillIDs, float currentEnergy)
        {
            enabledSkills.Value.Clear();
            enabledSkills.Value.UnionWith(skillIDs);
            UpdateSkillData();
            Energy = currentEnergy;
        }

        public void EnableSkill(string id)
        {
            Plugin.LoggerLog($"EnableSkill : {id}");
            if (enabledSkills.Value.Add(id))
                UpdateSkillData();
        }

        public void DisableSkill(string id)
        {
            List<string> removedIDs = new List<string>() { id };
            List<string> nextCheck = new List<string>() { id };

            while (removedIDs.Count > 0)
            {
                foreach (var item in removedIDs)
                {
                    enabledSkills.Value.Remove(item);
                    Plugin.LoggerLog($"DisableSkill : {item}");

                    foreach (var skill in enabledSkills.Value)
                    {
                        var skillInfo = SkillNodeLoader.loadedSkillNodes[skill];

                        if (!DMPSSkillTreeHelper.CheckAllConditions(skillInfo, this))
                            nextCheck.Add(skill);
                    }
                }

                removedIDs.Clear();
                foreach (var item in nextCheck)
                {
                    removedIDs.Add(item);
                }
                nextCheck.Clear();
            }

            UpdateSkillData();
        }

        private void UpdateSkillData()
        {
            SkillData.Update(enabledSkills.Value);
            Energy = Energy;
        }
    }
}
