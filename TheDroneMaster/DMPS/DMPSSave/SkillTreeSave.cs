using CustomSaveTx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public override string header => "DMPSSKILLTREESAVE";

        float _energy;
        public float Energy
        {
            get => _energy;
            set => _energy = Mathf.Clamp(value, 0, MaxEnergy);
        }
        public int activeDroneCount;
        HashSet<string> enabledSkills = new HashSet<string>();
        public DMPSBasicSave(SlugcatStats.Name name) : base(name)
        {
        }

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            if (string.IsNullOrEmpty(data))
                return;

            string[] items = Regex.Split(data, "<DMPS>");

            Energy = float.Parse(items[0]);
            activeDroneCount = int.Parse(items[1]);
            enabledSkills = JsonConvert.DeserializeObject<HashSet<string>>(items[2]);

            Plugin.LoggerLog($"LoadDatas : Energy={Energy}, activeDroneCount={activeDroneCount}, enabledSkills={string.Join(",", enabledSkills)}");
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied | saveAsIfPlayerQuit) return origSaveData;

            return $"{Energy}<DMPS>{activeDroneCount}<DMPS>{JsonConvert.SerializeObject(enabledSkills)}";
        }

        public bool CheckSkill(string id)
        {
            return enabledSkills.Contains(id);
        }

        public void EnableSkill(string id)
        {
            Plugin.LoggerLog($"EnableSkill : {id}");
            enabledSkills.Add(id);
        }

        public void DisableSkill(string id)
        {
            List<string> removedIDs = new List<string>() { id };
            List<string> nextCheck = new List<string>() { id };

            while (removedIDs.Count > 0)
            {
                foreach (var item in removedIDs)
                {
                    enabledSkills.Remove(item);
                    Plugin.LoggerLog($"DisableSkill : {item}");

                    foreach (var skill in enabledSkills)
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
