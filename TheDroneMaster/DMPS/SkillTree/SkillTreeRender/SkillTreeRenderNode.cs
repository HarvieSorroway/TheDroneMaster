using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.SkillTree.SkillTreeRender
{
    internal class SkillTreeRenderNode
    {
        SkillTreeRenderer renderer;
        
        public Vector2 setPos;

        public Vector2 pos, lastPos;

        protected FSprite bkg;

        public SkillTreeRenderNode(SkillTreeRenderer renderer, SkillTreeNode node)
        {
            this.renderer = renderer;
            setPos = node.renderInfo.renderPos;

            bkg = new FSprite("pixel")
            {
                scale = 70f,
                color = Custom.hexToColor("A33C5A"),
                rotation = 45f
            };

            renderer.container.AddChild(bkg);
        }

        public virtual void Update()
        {
            lastPos = pos;
            pos = setPos * renderer.scale + renderer.rootPos;
        }

        public virtual void Draw(float timeStacker)
        {
            bkg.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker));
        }   

        public virtual void Destroy()
        {
            bkg.RemoveFromContainer();
        }
    }
     
    internal class TestRendererNode : SkillTreeRenderNode
    {
        FSprite wave,rect;

        int counter, lastCounter;
        public TestRendererNode(SkillTreeRenderer renderer, SkillTreeNode node) : base(renderer, node)
        {
            wave = new FSprite("pixel")
            {
                scale = 70f,
                color = Custom.hexToColor("A33C5A"),
                rotation = 45f
            };
            
            renderer.container.AddChild(wave);
            bkg.MoveInFrontOfOtherNode(wave);

            rect = new FSprite("DMPS_SkillRect")
            {
            };
            renderer.container.AddChild(rect);
        }

        public override void Update()
        {
            base.Update();
            lastCounter = counter;
            counter++;
            if(counter == 40)
            {
                counter = lastCounter = 0;
            }
        }

        public override void Draw(float timeStacker)
        {
            float f = Mathf.Lerp(lastCounter / 40f, counter / 40f, timeStacker);
            base.Draw(timeStacker);
            wave.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker));
            rect.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker));
            wave.scale = Mathf.Lerp(70f, 140f, 1f -Mathf.Pow(1f - f, 4f));
            wave.alpha = Mathf.Pow(1f - f, 4f);
        }

        public override void Destroy()
        {
            base.Destroy();
            wave.RemoveFromContainer();
        }
    }
}
