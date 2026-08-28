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

        public float Energy
        {
            get => energy.Value;
            set => energy.Value = Mathf.Clamp(value, 0, MaxEnergy);
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
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            //Plugin.LoggerLog($"LoadDatas : Energy={Energy}, activeDroneCount={activeDroneCount}, enabledSkills={string.Join(",", enabledSkills.Value)}");
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            return string.Empty;
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
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
            Energy = currentEnergy;
        }

        public void EnableSkill(string id)
        {
            Plugin.LoggerLog($"EnableSkill : {id}");
            enabledSkills.Value.Add(id);
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
        }

    }

    /// <summary>
    /// 躯干技能部分
    /// </summary>
    internal partial class DMPSBasicSave : DeathPersistentSaveDataTx
    {
    }

    /// <summary>
    /// 无人机港技能部分
    /// </summary>
    internal partial class DMPSBasicSave : DeathPersistentSaveDataTx
    {
        public bool JetJump => CheckSkill("Skill.DronePortUpg.JetJump.Lv0");
        public float JetJumpCost
        {
            get
            {
                return 3f;
            }
        }
    }

    /// <summary>
    /// 燃烧室技能点部分
    /// </summary>
    internal partial class DMPSBasicSave : DeathPersistentSaveDataTx
    {
        public int MaxEnergy
        {
            get
            {
                return 50;
                //if (CheckSkill("Skill.BioReactorUpg.EnergyRegen.Lv3"))
                //    return 2.5f;
                //else if (CheckSkill("Skill.BioReactorUpg.EnergyRegen.Lv2"))
                //    return 2.0f;
                //else if (CheckSkill("Skill.BioReactorUpg.EnergyRegen.Lv1"))
                //    return 1.8f;
                //else if (CheckSkill("Skill.BioReactorUpg.EnergyRegen.Lv0"))
                //    return 1.5f;
                //else
                //    return 1.0f;
            }
        }

        public BioReactorType ReactorType
        {
            get
            {
                return BioReactorType.ThunderBolt;
            }
        }

        public enum BioReactorType
        {
            Default,
            OverDrive,
            ThunderBolt,
            Feedback,
        }
    }

    /// <summary>
    /// 无人机技能点部分
    /// </summary>
    internal partial class DMPSBasicSave : DeathPersistentSaveDataTx
    {
        public float DroneDmgMultiplier
        {
            get
            {
                if (CheckSkill("Skill.DroneUpg.DamageUpg.Lv3"))
                    return 2.5f;
                else if (CheckSkill("Skill.DroneUpg.DamageUpg.Lv2"))
                    return 2.0f;
                else if(CheckSkill("Skill.DroneUpg.DamageUpg.Lv1"))
                    return 1.8f;
                else if(CheckSkill("Skill.DroneUpg.DamageUpg.Lv0"))
                    return 1.5f;
                else
                    return 1.0f;
            }
        }

        public int DroneMaxCount
        {
            get
            {
                if (CheckSkill("Skill.DroneUpg.Count.Lv3"))
                    return 5;
                else if (CheckSkill("SSkill.DroneUpg.Count.Lv2"))
                    return 4;
                else if (CheckSkill("Skill.DroneUpg.Count.Lv1"))
                    return 3;
                else if (CheckSkill("Skill.DroneUpg.Count.Lv0"))
                    return 2;
                else
                    return 1;
            }
        }
    }
}
