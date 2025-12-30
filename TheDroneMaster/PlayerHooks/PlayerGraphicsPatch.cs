using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster
{
    public static class PlayerGraphicsPatch// hook player graphics to render some thing for dronemaster
    {
        public static void Patch()
        {
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;

            On.PlayerGraphics.Update += PlayerGraphics_Update;
        }


        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig.Invoke(self, ow);
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module))
            {
               module.InitExtraGraphics(self);
            }
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            bool getModule = false;
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module))
            {
                getModule = true;
                module.graphicsInited = false;
            }

            orig.Invoke(self, sLeaser, rCam);

            if (getModule)
            {
                //module.graphicsInited = true;

                //module.newEyeIndex = sLeaser.sprites.Length;
                //module.portIndex = module.newEyeIndex + 1;
                //module.grillIndex = module.portIndex + module.portGraphics.totalSprites;

                //Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1 + module.metalGills.totalSprites + module.portGraphics.totalSprites);

                //sLeaser.sprites[module.newEyeIndex] = new FSprite("FaceA0", true);
                //module.metalGills.startSprite = module.grillIndex;
                //module.metalGills.InitiateSprites(sLeaser, rCam);
                //module.portGraphics.startIndex = module.portIndex;
                //module.portGraphics.InitSprites(sLeaser, rCam);
                module.ExtraGraphicsInitSprites(self, sLeaser, rCam);
                self.AddToContainer(sLeaser, rCam, null);
            }
            else
            {
                //module.newEyeIndex = -1;
            }
        }


        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if(PlayerPatchs.modules.TryGetValue(self.player, out var module) && module.graphicsInited)
            {
                module.ExtraGraphicsAddToContainer(self, sLeaser, rCam, newContatiner);
            }
        }

        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(self, sLeaser, rCam, palette);
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module))
            {
                module.ExtraGraphicsApplyPalette(self, sLeaser, rCam, palette);
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module))
            {
                module.ExtraGraphicsUpdate(self);
            }
        }
        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module) && module.newEyeIndex != -1)
            {
                module.ExtraGraphicsDrawSprites(self, sLeaser, rCam, timeStacker, camPos);
            }
        }
    }
}
