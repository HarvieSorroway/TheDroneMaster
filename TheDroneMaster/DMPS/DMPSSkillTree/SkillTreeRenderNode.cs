using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
