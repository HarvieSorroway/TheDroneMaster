using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu;
using UnityEngine;

namespace TheDroneMaster.DMPS.PlayerHooks
{
    internal class TestUtil : PlayerModule.PlayerModuleUtil
    {
        bool lastGdown;
        public override void Update(Player player)
        {
            base.Update(player);
            bool Gdown = Input.GetKey(KeyCode.G);
            if(lastGdown != Gdown && Gdown && player.room != null)
            {
                SkillTreeMenu.OpenSkillTree(player.room.game);
            }
            lastGdown = Gdown;
        }
    }
}
