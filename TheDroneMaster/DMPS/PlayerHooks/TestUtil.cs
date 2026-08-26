using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSkillTree.SkillTreeMenu;
using TheDroneMaster.DMPS.DMPSutils;
using UnityEngine;

namespace TheDroneMaster.DMPS.PlayerHooks
{
    internal class TestUtil : PlayerModule.PlayerModuleUtil
    {
        bool lastGdown;
        public override void Update(Player player)
        {
            base.Update(player);
            if (player.room == null)
                return;
            bool Gdown = Input.GetKey(KeyCode.Mouse0);

            Vector2 mousePos = Input.mousePosition;
            var camPos = player.room.game.cameras[0].pos;
            var worldMousePos = mousePos + camPos;

            if (lastGdown != Gdown && Gdown && player.room != null)
            {
                float angle = Custom.AimFromOneVectorToAnother(player.firstChunk.pos, worldMousePos);

            player.room.AddObject(new ShockObject(player.room, player.firstChunk.pos, angle, 120f, 5f, 0.4f, source: player));
            }
            lastGdown = Gdown;
        }
    }
}
