using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.SkillTree
{
    internal struct SkillTreeNode
    {
        public string nodeID;
        public string[] relativeNodes;

        public SkillTreeNodeType nodeType;

        public float cost;
        public SkillTreeNodeUnlockConditionType unlockConditionType;
        public string[] extraConditionIDs;

        public SkillTreeNodeRenderInfo renderInfo;
    }

    internal struct SkillTreeNodeRenderInfo
    {
        public Vector2 renderPos;
        public float renderScale;
        public string renderSprite;

        public string displayName;
        public string description;
    }


    internal enum SkillTreeNodeType
    {
        BaseNode,
        Chain,
        ListBox
    }

    internal enum SkillTreeNodeUnlockConditionType
    {
        None,
        HasItem,
        HasCreature
    }
}
