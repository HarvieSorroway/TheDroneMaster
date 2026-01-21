using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster;
using TheDroneMaster.DMPS.DMPSPort;
using TheDroneMaster.DMPS.PlayerHooks;
using UnityEngine;

namespace DMPS.PlayerHooks
{
    internal class DMPSModule : PlayerModule
    {
        //port
        public DMPSDronePort port;

        //reactor energy
        public DMPSBioReactor bioReactor;

        //graphics
        public MetalGills metalGills;
        public int grillIndex = -1;
        public PSDronePortGraphics portGraphics;
        public int portIndex = -1;
        DMPSBioReactorGraphics reactorGraphics;
        public int reactorIndex = -1;

        JetJump jetJumpModule;

        public DMPSModule(Player player) : base(player)
        {
            jetJumpModule = AddUtil(new JetJump());
            port = AddUtil(new DMPSDronePort());
            bioReactor = new DMPSBioReactor(player);
            AddUtil(new TestUtil());
        }

        public override void InitExtraGraphics(PlayerGraphics playerGraphics)
        {
            metalGills = new MetalGills(playerGraphics, grillIndex);
            portGraphics = new PSDronePortGraphics(playerGraphics, portIndex, laserColor);
            reactorGraphics = new DMPSBioReactorGraphics(playerGraphics, laserColor, reactorIndex);

            jetJumpModule.IntoJetAction = null;
            jetJumpModule.ExitJetAction = null;

            jetJumpModule.IntoJetAction += portGraphics.IntoJetMode;
            jetJumpModule.ExitJetAction += portGraphics.ExitJetMode;
        }

        public override void ExtraGraphicsInitSprites(PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if(sLeaser.containers == null)
            {
                sLeaser.containers = new FContainer[1];
                sLeaser.containers[0] = new FContainer();
            }
            graphicsInited = true;

            newEyeIndex = sLeaser.sprites.Length;
            grillIndex = newEyeIndex + 1;
            portIndex = grillIndex + metalGills.totalSprites;
            reactorIndex = portIndex + portGraphics.totalSprites;

            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1 + metalGills.totalSprites + portGraphics.totalSprites + reactorGraphics.totalSprite);

            sLeaser.sprites[newEyeIndex] = new FSprite("FaceA0", true);
            metalGills.startSprite = grillIndex;
            metalGills.InitiateSprites(sLeaser, rCam);

            portGraphics.startIndex = portIndex;
            portGraphics.InitSprites(sLeaser, rCam);

            reactorGraphics.startIndex = reactorIndex;
            reactorGraphics.InitSprites(sLeaser, rCam);
        }

        public override void ExtraGraphicsAddToContainer(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if(newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Midground");

            newContatiner.AddChild(sLeaser.sprites[newEyeIndex]);
            sLeaser.sprites[newEyeIndex].MoveInFrontOfOtherNode(sLeaser.sprites[9]);

            metalGills.AddToContainer(sLeaser, rCam, newContatiner);
            portGraphics.AddToContainer(sLeaser, rCam, newContatiner);
            reactorGraphics.AddToContainer(sLeaser, rCam, newContatiner);

            if (sLeaser.containers != null)
            {
                foreach (FContainer fcontainer in sLeaser.containers)
                {
                    newContatiner.AddChild(fcontainer);
                }
                sLeaser.containers[0].MoveToBack();
            }
        }

        public override void ExtraGraphicsApplyPalette(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            metalGills.SetGillColors(bodyColor, laserColor, eyeColor);
            metalGills.ApplyPalette(sLeaser, rCam, palette);
            portGraphics.ApplyPalette(sLeaser, rCam, palette);
            reactorGraphics.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void ExtraGraphicsUpdate(PlayerGraphics self)
        {
            metalGills.Update();
            portGraphics.Update();
            reactorGraphics.Update();
            reactorGraphics.energy = bioReactor.EnergyPercentage;
        }

        public override void ExtraGraphicsDrawSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Color color = eyeColor;
            if (PlayerDroneHUD.instance != null)
                color = Color.Lerp(eyeColor, laserColor, PlayerDroneHUD.instance.alpha);

            sLeaser.sprites[newEyeIndex].color = color;

            sLeaser.sprites[newEyeIndex].element = sLeaser.sprites[9].element;
            sLeaser.sprites[newEyeIndex].SetPosition(sLeaser.sprites[9].x, sLeaser.sprites[9].y);
            sLeaser.sprites[newEyeIndex].scaleX = sLeaser.sprites[9].scaleX;
            sLeaser.sprites[newEyeIndex].scaleY = sLeaser.sprites[9].scaleY;
            sLeaser.sprites[newEyeIndex].rotation = sLeaser.sprites[9].rotation;
            sLeaser.sprites[newEyeIndex].isVisible = sLeaser.sprites[9].isVisible;

            metalGills.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            portGraphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            reactorGraphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public void HypothermiaUpdate(Player player)
        {
           bioReactor.HypothermiaUpdate(player);
        }
    }
}
