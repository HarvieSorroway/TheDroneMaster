using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.SkillTree.SkillTreeRender
{
    internal class SkillTreeRenderer
    {
        public FContainer container = new FContainer();
        SkillTreeLogicBase renderLogic;

        List<SkillTreeRenderNode> nodeRenderers = new List<SkillTreeRenderNode>();

        //render param
        public Vector2 rootPos = Custom.rainWorld.options.ScreenSize / 2f;
        public float scale;

        public void Update()
        {
            for (int i = nodeRenderers.Count - 1; i >= 0; i--)
            {
                nodeRenderers[i].Update();
            }
        }

        public void Draw(float timeStacker)
        {
            for(int i = nodeRenderers.Count - 1; i >= 0; i--)
            {
                nodeRenderers[i].Draw(timeStacker);
            }
        }

        public SkillTreeRenderNode CreateNodeRenderer(SkillTreeNode skillTreeNode)
        {
            //var node = new SkillTreeRenderNode(this, skillTreeNode);
            var node = new TestRendererNode(this, skillTreeNode);
            nodeRenderers.Add(node);
            return node;
        }
    }

}
