using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster;
using UnityEngine;

namespace DMPS.PlayerHooks
{
    public class DMPSModule : TheDroneMaster.PlayerModule
    {
        //graphics
        public MetalGills metalGills;
        public int grillIndex = -1;


        public DMPSModule(Player player) : base(player)
        {
        }

        public override void InitExtraGraphics(PlayerGraphics playerGraphics)
        {
            metalGills = new MetalGills(playerGraphics, grillIndex);
        }

        public override void ExtraGraphicsInitSprites(PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            graphicsInited = true;

            newEyeIndex = sLeaser.sprites.Length;
            grillIndex = newEyeIndex + 1;

            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1 + metalGills.totalSprites);

            sLeaser.sprites[newEyeIndex] = new FSprite("FaceA0", true);
            metalGills.startSprite = grillIndex;
            metalGills.InitiateSprites(sLeaser, rCam);
        }

        public override void ExtraGraphicsAddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            FContainer container = rCam.ReturnFContainer("Midground");

            container.AddChild(sLeaser.sprites[newEyeIndex]);
            sLeaser.sprites[newEyeIndex].MoveInFrontOfOtherNode(sLeaser.sprites[9]);

            metalGills.AddToContainer(sLeaser, rCam, container);
        }

        public override void ExtraGraphicsApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            metalGills.SetGillColors(bodyColor, laserColor, eyeColor);
            metalGills.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void ExtraGraphicsUpdate(PlayerGraphics self)
        {
            metalGills.Update();
        }

        public override void ExtraGraphicsDrawSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Color color = eyeColor;
            if (DroneHUD.instance != null)
                color = Color.Lerp(eyeColor, laserColor, DroneHUD.instance.alpha);

            sLeaser.sprites[newEyeIndex].color = color;

            sLeaser.sprites[newEyeIndex].element = sLeaser.sprites[9].element;
            sLeaser.sprites[newEyeIndex].SetPosition(sLeaser.sprites[9].x, sLeaser.sprites[9].y);
            sLeaser.sprites[newEyeIndex].scaleX = sLeaser.sprites[9].scaleX;
            sLeaser.sprites[newEyeIndex].scaleY = sLeaser.sprites[9].scaleY;
            sLeaser.sprites[newEyeIndex].rotation = sLeaser.sprites[9].rotation;
            sLeaser.sprites[newEyeIndex].isVisible = sLeaser.sprites[9].isVisible;

            metalGills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }
    }
}
