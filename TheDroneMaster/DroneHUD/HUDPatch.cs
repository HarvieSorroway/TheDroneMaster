using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public static class HUDPatch
    {
        public static RoomCamera currentCam;
        public static void Patch()
        {
            On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;
        }

        private static void RoomCamera_FireUpSinglePlayerHUD(On.RoomCamera.orig_FireUpSinglePlayerHUD orig, RoomCamera self, Player player)
        {
            orig.Invoke(self, player);
            currentCam = self;
            if(PlayerPatchs.modules.TryGetValue(player,out var module) && module.ownDrones)
            {
                Debug.Log("FireUpPlayerHUD" + module.port.ToString());
                self.hud.AddPart(new PlayerDroneHUD(self.hud,module.port));
            }
        }
    }
}
