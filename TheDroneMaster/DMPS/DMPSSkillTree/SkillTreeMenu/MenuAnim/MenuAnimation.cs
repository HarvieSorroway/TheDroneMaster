using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu.MenuAnim
{
    internal class MenuAnimation
    {
        public SkillTreeMenu menu;
        float duration;
        public float progression, lastProgression;
        public bool Finished => progression == 1f;

        public MenuAnimation(SkillTreeMenu menu, int duration)
        {
            this.menu = menu;
            this.duration = duration;
        }

        public virtual void Update()
        {
            lastProgression = progression;
            progression = Mathf.Clamp01(progression + 1f / duration);
        }

        public virtual void Finish()
        {

        }
    }
}
