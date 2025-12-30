using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSDrone.DroneWeapon
{
    internal class BaseDroneWeapon
    {
        public bool weaponEnable;
        public virtual void Update(DMPSDrone drone)
        {

        }

        public virtual BaseDroneWeaponGraphics InitGraphics(DMPSDroneGraphics graphics, int startSprite)
        {
            throw new NotImplementedException();
        }

        public virtual void FireWeapon(DMPSDrone drone)
        {
            if(drone.graphicsModule != null)
            {
                (drone.graphicsModule as DMPSDroneGraphics).weaponGraphics.WeaponFired();
            }
        }
    }

    internal class BaseDroneWeaponGraphics
    {
        public readonly DMPSDroneGraphics graphics;
        public readonly int startSprite;

        public int laserPanelAIndex => startSprite;
        public int laserPanelBIndex => startSprite + 1;

        public virtual int totSprites => 2;

        public BaseDroneWeaponGraphics(DMPSDroneGraphics graphics, int startSprite)
        {
            this.graphics = graphics;
            this.startSprite = startSprite;
        }

        public virtual void Update(DMPSDroneGraphics graphics)
        {

        }

        public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[laserPanelAIndex].color = Color.white;
            sLeaser.sprites[laserPanelBIndex].color = graphics.laserColor;
        }

        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 bodyPos, Vector2 dir, float tiltSin, float tiltCos, float rotation, float scaleXFactor, float scaleYFactor)
        {
            sLeaser.sprites[laserPanelAIndex].SetPosition(bodyPos - camPos);
            sLeaser.sprites[laserPanelAIndex].rotation = rotation + 90f;
            sLeaser.sprites[laserPanelAIndex].scaleY = 0.8f * scaleYFactor;
            sLeaser.sprites[laserPanelAIndex].scaleX = tiltSin * 0.8f * scaleXFactor;
            sLeaser.sprites[laserPanelBIndex].SetPosition(bodyPos - camPos);
            sLeaser.sprites[laserPanelBIndex].rotation = rotation + 90f;
            sLeaser.sprites[laserPanelBIndex].scaleY = scaleYFactor;
            sLeaser.sprites[laserPanelBIndex].scaleX = tiltSin * scaleXFactor;
        }

        public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[laserPanelBIndex] = new FSprite("Circle20", true);
            sLeaser.sprites[laserPanelAIndex] = new FSprite("Circle20", true);
        }

        public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.sprites[laserPanelBIndex].MoveInFrontOfOtherNode(sLeaser.sprites[DMPSDroneGraphics.bodySegmentIndex]);
            for (int i = 0; i < graphics.wings.Length; i++)
            {
                sLeaser.sprites[laserPanelBIndex].MoveInFrontOfOtherNode(sLeaser.sprites[graphics.wings[i].startIndex]);
            }

            sLeaser.sprites[laserPanelAIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPanelBIndex]);
            //sLeaser.sprites[gunPointerIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPannelAIndex]);
        }

        public virtual void WeaponFired()
        {

        }
    }
}
