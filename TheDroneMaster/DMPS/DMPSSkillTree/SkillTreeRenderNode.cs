using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static TheDroneMaster.DMPS.DMPSSkillTree.SkillNode;

namespace TheDroneMaster.DMPS.DMPSSkillTree
{
    internal struct SkillTreeRenderNode
    {
        public string renderNodeIDInfo, bindSkillNodeInfo, iconSprite;

        public Vector2[] posInfo;
        public float scaleInfo;
        public SkillTreeRenderType typeInfo;

        public string[] subRenderNodeInfo;
        public int layer;

        public SkillTreeRenderNodeExtCondition[] extConditions;
    }

    internal struct SkillTreeRenderNodeExtCondition
    {
        public SkillTreeRenderNodeExtConditionType type;
        public ConditionBoolType boolType;
        public ConditionType conditionType;
        public string info;
    }

    /// <summary>
    /// 渲染节点的额外条件控制，主要用于渲染连线节点
    /// </summary>
    internal enum SkillTreeRenderNodeExtConditionType
    {
        Pre,
        Show,
        Hide,
    }

    internal enum SkillTreeRenderType
    {
        StaticNode,
        BasicNode,
        LineNode,
        SubBasicNode,
        SubSingleSelectNode,
        NodeGroup
    }
}
