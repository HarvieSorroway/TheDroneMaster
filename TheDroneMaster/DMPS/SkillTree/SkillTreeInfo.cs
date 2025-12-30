using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.SkillTree
{
    internal static class SkillTreeInfo
    {
        public static List<SkillTreeNode> allNodes = new List<SkillTreeNode>();

        public static void ReadTreeInfo()
        {
            allNodes.Clear();
            allNodes.Add(new SkillTreeNode()
            {
                nodeID = "skill_node_1",
                nodeType = SkillTreeNodeType.BaseNode,
                unlockConditionType = SkillTreeNodeUnlockConditionType.None,
                cost = 1f,
                renderInfo = new SkillTreeNodeRenderInfo()
                {
                    renderPos = new UnityEngine.Vector2(0f, 0f),
                    renderScale = 1f,
                    renderSprite = "",
                    displayName = "Skill Node 1",
                    description = "This is the first skill node."
                }
            });
        }
    }
}
