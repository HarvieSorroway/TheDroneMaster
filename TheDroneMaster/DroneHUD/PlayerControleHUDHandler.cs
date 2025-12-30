using System;

namespace TheDroneMaster.DroneHUD
{
    public class PlayerControleHUDHandler
    {
        public static void Init()
        {
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if(!PlayerPatchs.modules.TryGetValue(self, out var module))return;
            
        }

    }
}
