using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.DMPSSkillTree
{
    internal struct SkillNode
    {
        public string skillID;

        public float cost;

        public List<SKillNodeConditionInfo> conditions;

        public Dictionary<string, SkillNodeDescriptionInfo> descriptionInfos;

        public bool TryGetDescriptionInfo(InGameTranslator.LanguageID languageID, out SkillNodeDescriptionInfo res)
        {
            if (descriptionInfos.TryGetValue(languageID.value, out res))
                return true;
            else if (descriptionInfos.TryGetValue(InGameTranslator.LanguageID.English.value, out res))
                return true;
            return false;
        }

        internal struct SkillNodeDescriptionInfo
        {
            public string name;
            public string description;
        }

        internal struct SKillNodeConditionInfo
        {
            public ConditionType type;
            public ConditionBoolType boolType;
            public string info;
        }

        internal enum ConditionType
        {
            Item,
            SkillNode
        }

        internal enum ConditionBoolType
        {
            NotAnd,
            NotOr,
            And,
            Or
        }
    }

    
}
