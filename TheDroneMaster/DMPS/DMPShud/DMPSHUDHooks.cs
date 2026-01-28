using DMPS.PlayerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPShud.EnergyBar;

namespace TheDroneMaster.DMPS.DMPShud
{
    internal static class DMPSHUDHooks
    {
        public static void HooksOn()
        {
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
        }

        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig.Invoke(self, cam);
            if (PlayerPatchs.modules.TryGetValue((self.owner as Player), out var m) && m is DMPSModule module)
            {
                //self.AddPart(new ReactorEnergyBar(self));
                self.AddPart(new HUDEnergyBar(self));
            }
        }
    }
}
