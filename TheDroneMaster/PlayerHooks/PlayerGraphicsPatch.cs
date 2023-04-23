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
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module) && module.ownDrones)
            {
                module.metalGills = new MetalGills(self, module.grillIndex);
                module.portGraphics = new DronePortGraphics(self, module.portIndex);
            }
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            bool getModule = false;
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module) && module.ownDrones)
            {
                getModule = true;
                module.graphicsInited = false;
            }
            orig.Invoke(self, sLeaser, rCam);

            if (getModule)
            {
                module.graphicsInited = true;

                module.newEyeIndex = sLeaser.sprites.Length;
                module.portIndex = module.newEyeIndex + 1;
                module.grillIndex = module.portIndex + module.portGraphics.totalSprites;

                Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1 + module.metalGills.totalSprites + module.portGraphics.totalSprites);

                sLeaser.sprites[module.newEyeIndex] = new FSprite("FaceA0", true);
                module.metalGills.startSprite = module.grillIndex;
                module.metalGills.InitiateSprites(sLeaser, rCam);
                module.portGraphics.startIndex = module.portIndex;
                module.portGraphics.InitSprites(sLeaser, rCam);

                self.AddToContainer(sLeaser, rCam, null);
            }
            else
            {
                module.newEyeIndex = -1;
            }
        }


        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if(PlayerPatchs.modules.TryGetValue(self.player, out var module) && module.ownDrones && module.graphicsInited)
            {
                FContainer container = rCam.ReturnFContainer("Midground");

                container.AddChild(sLeaser.sprites[module.newEyeIndex]);
                sLeaser.sprites[module.newEyeIndex].MoveInFrontOfOtherNode(sLeaser.sprites[9]);

                module.metalGills.AddToContainer(sLeaser, rCam, container);
                module.portGraphics.AddToContainer(sLeaser, rCam, container);
            }
        }

        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(self, sLeaser, rCam, palette);
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module) && module.ownDrones)
            {
                module.metalGills.SetGillColors(module.bodyColor, module.laserColor,module.eyeColor);
                module.metalGills.ApplyPalette(sLeaser, rCam, palette);

                module.portGraphics.ApplyPalette(sLeaser, rCam, palette);
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (PlayerPatchs.modules.TryGetValue(self.player, out var module) && module.ownDrones)
            {
                module.metalGills.Update();
                module.portGraphics.Update();
            }
        }
        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (PlayerPatchs.modules.TryGetValue(self.player,out var module) && module.ownDrones && DroneHUD.instance != null && module.newEyeIndex != -1)
            {
                Color color = Color.Lerp(module.eyeColor, module.laserColor, DroneHUD.instance.alpha);
                sLeaser.sprites[module.newEyeIndex].color = color;

                sLeaser.sprites[module.newEyeIndex].element = sLeaser.sprites[9].element;
                sLeaser.sprites[module.newEyeIndex].SetPosition(sLeaser.sprites[9].x, sLeaser.sprites[9].y);
                sLeaser.sprites[module.newEyeIndex].scaleX = sLeaser.sprites[9].scaleX;
                sLeaser.sprites[module.newEyeIndex].scaleY = sLeaser.sprites[9].scaleY;
                sLeaser.sprites[module.newEyeIndex].rotation = sLeaser.sprites[9].rotation;
                sLeaser.sprites[module.newEyeIndex].isVisible = sLeaser.sprites[9].isVisible;

                module.metalGills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                module.portGraphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
        }
    }
}
