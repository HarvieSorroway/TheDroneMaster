using RWCustom;
using System.Collections.Generic;
using System.Linq;
using TheDroneMaster.DMPS.DMPSutils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSDrone.DroneWeapon
{
    internal class ShockTrainGun : BaseDroneWeapon
    {
        const int chargeCD = 40 * 3, chargingCounter = 40 * 3;

        internal int charging, cd;
        internal float ChargingF => (float)charging / chargingCounter;

        public override void Update(DMPSDrone drone)
        {
            bool firingConditionMatched = ((drone.dir - drone.weaponTargetDir).magnitude < 0.06f) && cd == 0;
            if (drone.UsingWeapon && cd == 0)
            {
                foreach (var chunk in drone.AI.target.realizedCreature.bodyChunks)
                {
                    firingConditionMatched = firingConditionMatched && drone.room.VisualContact(drone.firstChunk.pos, chunk.pos);
                }
            }
            weaponEnable = firingConditionMatched;

            if (drone.UsingWeapon && charging < chargingCounter && cd == 0)
                charging++;
            else if (charging > 0)
                charging--;

            if (charging == chargingCounter && weaponEnable)
                FireWeapon(drone);

            if (cd > 0)
                cd--;
        }
        public override void FireWeapon(DMPSDrone drone)
        {
            base.FireWeapon(drone);
            for(int i = 0;i < 2; i++)
            {
                drone.room.AddObject(new ShockObject(drone.room, drone.firstChunk.pos, Custom.VecToDeg(drone.dir), 80f, 5f, 0.4f, source: drone));
            }
            drone.room.PlaySound(DMEnums.DMPS.Sound.DMPS_ShootFuse, drone.firstChunk, false, 0.2f, 1f + Random.value * 0.1f);

            for(int i = 0;i < Random.Range(10, 20); i++)
            {
                drone.room.AddObject(new Spark(drone.DangerPos, Custom.RNV() * Random.Range(5f, 10f) + drone.dir * Random.Range(10f, 20f), Color.Lerp(LaserDroneGraphics.defaultLaserColor, Color.white, Random.value * 0.3f), null, 20, 40));
            }

            cd = chargeCD;
            charging = 0;
        }

        public override BaseDroneWeaponGraphics InitGraphics(DMPSDroneGraphics graphics, int startSprite)
        {
            return new ShockTrainGunGraphics(graphics, startSprite);
        }
    }

    internal class ShockTrainGunGraphics : BaseDroneWeaponGraphics
    {
        const float bladeLength = 15f, bladeWidth = 5f, bladeGap = 2f, maxAngularVel = 360f * 3f;
        const int shockTrainBlades = 3;

        public override int totSprites => 2 + shockTrainBlades;

        public int PanelSprite => startSprite;

        public int FlashSprite => startSprite + 1;
        public int BladeSprite => startSprite + 2;


        float rotation;
        float angularVelocity;
        float bladeSpreadAngle;

        float burst, lastBurst;

        Color palBlack;

        public ShockTrainGunGraphics(DMPSDroneGraphics graphics, int startSprite) : base(graphics, startSprite)
        {
            bladeSpreadAngle = 360f / shockTrainBlades;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[PanelSprite] = new FSprite("Circle20", true);
            sLeaser.sprites[FlashSprite] = new FSprite("DMPS_JetFlare", true)
            {
                color = LaserDroneGraphics.defaultLaserColor,
                alpha = 0,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
            };

            for (int i = 0; i < shockTrainBlades; i++)
            {
                sLeaser.sprites[BladeSprite + i] = new CustomFSprite("pixel");
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            palBlack = palette.blackColor;

            sLeaser.sprites[PanelSprite].color = palette.blackColor;
            for(int i = 0;i < shockTrainBlades; i++)
            {
                (sLeaser.sprites[BladeSprite + i] as CustomFSprite).verticeColors = new Color[4]
                {
                    palette.blackColor,palette.blackColor,palette.blackColor,palette.blackColor
                };
            }
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.sprites[PanelSprite].MoveInFrontOfOtherNode(sLeaser.sprites[DMPSDroneGraphics.bodySegmentIndex]);
            for (int i = 0; i < graphics.wings.Length; i++)
            {
                sLeaser.sprites[PanelSprite].MoveInFrontOfOtherNode(sLeaser.sprites[graphics.wings[i].startIndex]);
            }

            for(int i = 0;i < shockTrainBlades; i++)
                sLeaser.sprites[BladeSprite + i].MoveInFrontOfOtherNode(sLeaser.sprites[PanelSprite]);

            sLeaser.sprites[FlashSprite].RemoveFromContainer();
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[FlashSprite]);      
        }

        public override void Update(DMPSDroneGraphics graphics)
        {
            base.Update(graphics);
            var weapon = (graphics.drone.weapon as ShockTrainGun);

            rotation = GetRotation(rotation - angularVelocity / 40f);

            if(weapon.charging > 0 && weapon.cd == 0)
                angularVelocity = Mathf.Clamp(angularVelocity + 20f, 0f, maxAngularVel);
            else
                angularVelocity = Mathf.Clamp(angularVelocity - 40f, 0f, maxAngularVel);

            lastBurst = burst;
            if (burst > 0f)
                burst = Mathf.Max(0f, burst - 1 / 20f);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 bodyPos, Vector2 dir, float tiltSin, float tiltCos, float rotation, float scaleXFactor, float scaleYFactor)
        {
            var weapon = (graphics.drone.weapon as ShockTrainGun);
            float smoothBurst = DMHelper.EaseInOutCubic(Mathf.Lerp(lastBurst, burst, timeStacker));

            sLeaser.sprites[PanelSprite].SetPosition(bodyPos - camPos);
            sLeaser.sprites[PanelSprite].rotation = rotation + 90f;
            sLeaser.sprites[PanelSprite].scaleY = scaleYFactor;
            sLeaser.sprites[PanelSprite].scaleX = tiltSin * scaleXFactor;

            sLeaser.sprites[FlashSprite].SetPosition(bodyPos + dir * bladeLength * 0.5f - camPos);
            sLeaser.sprites[FlashSprite].rotation = rotation + 90f;
            sLeaser.sprites[FlashSprite].alpha = weapon.ChargingF * Random.value * 0.7f + smoothBurst * 0.5f;
            sLeaser.sprites[FlashSprite].scale = weapon.ChargingF * 0.8f + Random.value * 0.4f + smoothBurst;

            Vector2 perpDir = Custom.PerpendicularVector(dir);
            float smoothBaseRotation = GetRotation(rotation + this.rotation - angularVelocity * timeStacker / 40f);//预测形式的平滑过渡
            float basePerpDir = Custom.VecToDeg(perpDir);

            
            Color burstCol = Color.Lerp(palBlack, LaserDroneGraphics.defaultLaserColor, smoothBurst);
            Color chargeCol = Color.Lerp(burstCol, LaserDroneGraphics.defaultLaserColor, weapon.ChargingF);

            for (int i = 0;i < shockTrainBlades; i++)
            {
                Vector2 bladePerpDir = Custom.DegToVec(GetRotation(smoothBaseRotation + basePerpDir + i * bladeSpreadAngle));
                float alongX = Vector2.Dot(bladePerpDir, dir) * tiltSin;
                float alongY = Vector2.Dot(bladePerpDir, perpDir);

                bladePerpDir = dir * alongX + perpDir * alongY;

                var cSprite = sLeaser.sprites[BladeSprite + i] as CustomFSprite;
                cSprite.MoveVertice(0, bodyPos + bladePerpDir * bladeGap - camPos);
                cSprite.MoveVertice(1, bodyPos + bladePerpDir * (bladeWidth + bladeGap) - camPos);
                cSprite.MoveVertice(2, bodyPos + bladePerpDir * (bladeWidth + bladeGap) + dir * (bladeLength) - camPos);
                cSprite.MoveVertice(3, bodyPos + bladePerpDir * bladeGap + dir * bladeLength - camPos);

                cSprite.verticeColors[0] = chargeCol;
                cSprite.verticeColors[1] = burstCol;
                cSprite.verticeColors[2] = burstCol;
                cSprite.verticeColors[3] = chargeCol;
            }
        }

        float GetRotation(float orig)
        {
            return orig % 360f;
        }

        public override void WeaponFired()
        {
            base.WeaponFired();
            burst = lastBurst = 1f;
        }
    }
}
