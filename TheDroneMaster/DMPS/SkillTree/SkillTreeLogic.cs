using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.SkillTree.SkillTreeRender;

namespace TheDroneMaster.DMPS.SkillTree
{

    internal class SkillTreeLogicBase
    {
        public SkillTreeLogicBase()
        {
        }

        public virtual void InitRenderer(SkillTreeRenderer renderer)
        {
            foreach(var nodeInfo in SkillTreeInfo.allNodes)
            {
                renderer.CreateNodeRenderer(nodeInfo);
            }
        }

        public virtual void Update()
        {
        }
    }
}
