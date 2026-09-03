using System.Collections.Generic;

namespace TheDroneMaster.DMPS.DMPSSave
{
    /// <summary>
    /// 由当前已启用技能计算出的游戏参数。
    /// </summary>
    internal sealed class DMPSSkillData
    {
        public bool JetJump { get; private set; }
        public float JetJumpCost { get; private set; }
        public int MaxEnergy { get; private set; }
        public BioReactorType ReactorType { get; private set; }
        public float DroneDmgMultiplier { get; private set; }
        public int DroneMaxCount { get; private set; }

        public DMPSSkillData()
        {
            Update(new HashSet<string>());
        }

        /// <summary>
        /// 根据当前已启用技能重新计算全部技能数据。
        /// </summary>
        public void Update(IEnumerable<string> enabledSkills)
        {
            HashSet<string> skills = enabledSkills as HashSet<string>
                ?? new HashSet<string>(enabledSkills);

            JetJump = skills.Contains("Skill.DronePortUpg.JetJump.Lv0");
            JetJumpCost = 3f;

            MaxEnergy = 50;
            ReactorType = BioReactorType.ThunderBolt;

            if (skills.Contains("Skill.DroneUpg.DamageUpg.Lv3"))
                DroneDmgMultiplier = 2.5f;
            else if (skills.Contains("Skill.DroneUpg.DamageUpg.Lv2"))
                DroneDmgMultiplier = 2.0f;
            else if (skills.Contains("Skill.DroneUpg.DamageUpg.Lv1"))
                DroneDmgMultiplier = 1.8f;
            else if (skills.Contains("Skill.DroneUpg.DamageUpg.Lv0"))
                DroneDmgMultiplier = 1.5f;
            else
                DroneDmgMultiplier = 1.0f;

            if (skills.Contains("Skill.DroneUpg.Count.Lv3"))
                DroneMaxCount = 5;
            else if (skills.Contains("Skill.DroneUpg.Count.Lv2"))
                DroneMaxCount = 4;
            else if (skills.Contains("Skill.DroneUpg.Count.Lv1"))
                DroneMaxCount = 3;
            else if (skills.Contains("Skill.DroneUpg.Count.Lv0"))
                DroneMaxCount = 2;
            else
                DroneMaxCount = 1;
        }

        public enum BioReactorType
        {
            Default,
            OverDrive,
            ThunderBolt,
            Feedback,
        }
    }
}
