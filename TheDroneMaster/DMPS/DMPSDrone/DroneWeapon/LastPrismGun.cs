using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSDrone.DroneWeapon
{
    internal class LastPrismGun : BaseDroneWeapon
    {
        public override BaseDroneWeaponGraphics InitGraphics(DMPSDroneGraphics graphics, int startSprite)
        {
            return new LastPrismGraphics(graphics, startSprite);
        }

        public override void Update(DMPSDrone drone)
        {
            base.Update(drone);
            if(drone.UsingWeapon)
                FireWeapon(drone);
        }

        public override void FireWeapon(DMPSDrone drone)
        {
            base.FireWeapon(drone);

            Vector2 pos = Custom.RectCollision(drone.firstChunk.pos, drone.firstChunk.pos + drone.dir * 10000f, drone.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);

            var collided = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(drone.room, drone.firstChunk.pos, pos);

            if (collided != null)
            {
                pos = drone.firstChunk.pos + drone.weaponTargetDir * Vector2.Distance(drone.firstChunk.pos, drone.room.MiddleOfTile(collided.Value));
            }

            SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(drone, drone.room, drone.firstChunk.pos, ref pos, 1f, 1, null, true);
            if (result.hitSomething)
            {
                (result.chunk.owner as Creature).Violence(drone.firstChunk, drone.weaponTargetDir, result.chunk, result.onAppendagePos, Creature.DamageType.Electric, 0.01f, 1f);
                //drone.room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, LaserDroneGraphics.defaultLaserColor));
            }
        }
    }

    internal class LastPrismGraphics : BaseDroneWeaponGraphics
    {
        static float maxRotateVel = 2880 / 40f;
        static int maxCharge = 400;
        static int beamsCount = 6;
        int chargingLast, currentCharge, lastCharge;
        float lastRotate, rotate;

        PrismBeam[] beams;

        public int gunPointerIndex => startSprite + 2;
        public override int totSprites => base.totSprites + 1 + beamsCount * PrismBeam.totSprites;
        public LastPrismGraphics(DMPSDroneGraphics graphics, int startSprite) : base(graphics, startSprite)
        {
            beams = new PrismBeam[beamsCount];
            for (int i = 0; i < beams.Length; i++)
            {
                beams[i] = new PrismBeam(this, i * PrismBeam.totSprites + gunPointerIndex + 1, i / (float)beamsCount, 360f * (i / (float)beamsCount));
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites[gunPointerIndex] = new FSprite("pixel", true);
            for (int i = 0; i < beams.Length; i++)
                beams[i].InitiateSprites(sLeaser, rCam);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
            sLeaser.sprites[gunPointerIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPanelAIndex]);
            for (int i = 0; i < beams.Length; i++)
                beams[i].AddToContainer(sLeaser, rCam, newContatiner);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            sLeaser.sprites[gunPointerIndex].color = graphics.laserColor;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 bodyPos, Vector2 dir, float tiltSin, float tiltCos, float rotation, float scaleXFactor, float scaleYFactor)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos, bodyPos, dir, tiltSin, tiltCos, rotation, scaleXFactor, scaleYFactor);
            //float rotA = Mathf.Lerp(lastRotate, rotate, timeStacker);
            //float rotB = Mathf.Lerp(lastRotate - 360f, rotate, timeStacker);
            //float rotC = Mathf.Lerp(lastRotate + 360f, rotate, timeStacker);

            //float delta = (rotate - rotA), smoothRotate = rotA;
            //if ((rotate - rotB) < delta)
            //{
            //    delta = rotate - rotB;
            //    smoothRotate = rotB;
            //}
            //if((rotate - rotC) < delta)
            //{
            //    delta = rotate - rotC;
            //    smoothRotate = rotC;
            //}
            float smoothRotate = Mathf.Lerp(lastRotate, rotate, timeStacker);

            float smoothCharge = Mathf.InverseLerp(0f, maxCharge, Mathf.Lerp(lastCharge, currentCharge, timeStacker)) * (chargingLast > 0 ? 1f : 0f);

            sLeaser.sprites[gunPointerIndex].SetPosition(bodyPos + dir * (DMPSDroneGraphics.gunLength * scaleXFactor * tiltCos / 2f - DMPSDroneGraphics.gunWidth * scaleYFactor / 2f) - camPos);
            sLeaser.sprites[gunPointerIndex].scaleX = tiltCos * scaleXFactor * DMPSDroneGraphics.gunLength;
            sLeaser.sprites[gunPointerIndex].scaleY = DMPSDroneGraphics.gunWidth * scaleYFactor;
            sLeaser.sprites[gunPointerIndex].rotation = rotation + 90f;


            for (int i = 0; i < beams.Length; i++)
                beams[i].DrawSprites(sLeaser, rCam, timeStacker, camPos, smoothCharge, bodyPos, dir, smoothRotate);
        }

        public override void Update(DMPSDroneGraphics graphics)
        {
            base.Update(graphics);

            lastCharge = currentCharge;
            if (chargingLast > 0)
            {
                chargingLast--;

                if (currentCharge < maxCharge)
                    currentCharge++;
            }
            else
            {
                currentCharge = 0;
                rotate = lastRotate = 0f;
            }

            lastRotate = rotate;
            if(currentCharge > 0)
            {
                rotate += Mathf.Lerp(0.1f, 1f, Mathf.Pow( Mathf.InverseLerp(0f, maxCharge - 80, currentCharge), 4f)) * maxRotateVel;
                //rotate = rotate % 360f;
            }
        }

        public override void WeaponFired()
        {
            base.WeaponFired();
            chargingLast = 2;
        }

        class PrismBeam
        {
            public static int totSprites = 2;
            public static float width = 5f;

            LastPrismGraphics graphics;
            int index;

            float deltaAngle = 70f;
            float rotateBias;

            Color color;

            public PrismBeam(LastPrismGraphics graphics, int index, float hue, float timerBias)
            {
                this.graphics = graphics;
                this.index = index;
                color = Custom.HSL2RGB(hue, 1f, 0.5f);
                this.rotateBias = timerBias;
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                sLeaser.sprites[index].RemoveFromContainer();
                sLeaser.sprites[index + 1].RemoveFromContainer();
                rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[index]);
                rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[index + 1]);
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, float charge, Vector2 basePos, Vector2 gunDir, float rotate)
            {
                var weapon = (graphics.graphics.drone.weapon as DefaultLaserGun);
                var room = graphics.graphics.drone.room;

                if (room == null)
                    return;

                float smoothSpread = 1f - DMHelper.EaseInOutCubic(Mathf.Clamp01(charge * 1.2f));

                rotate = (rotate + rotateBias);
                float sin = Mathf.Sin(rotate * Mathf.Deg2Rad);

                float rotation = Custom.VecToDeg(gunDir) - smoothSpread * deltaAngle * sin;
                
                Vector2 dir = Custom.DegToVec(rotation);
                Vector2 perpBias = Custom.PerpendicularVector(gunDir) * sin * width / 2f;

                basePos += perpBias;

                Vector2 pos = Custom.RectCollision(basePos, basePos + dir * 10000f, room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                var collided = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, basePos, pos);

                if (collided != null)
                {
                    pos = basePos + dir * Vector2.Distance(basePos, room.MiddleOfTile(collided.Value));
                }

                SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(graphics.graphics.drone, room, basePos, ref pos, 2f, 1, null, true);
                if (result.hitSomething)
                    pos = result.collisionPoint;

                //float flash = Mathf.Clamp01(Random.value * (1f - smoothSpread) + burst);
                float alpha = Mathf.Pow(charge, 2f);

                sLeaser.sprites[index].SetPosition((pos + basePos) / 2f - camPos);
                sLeaser.sprites[index].rotation = rotation;
                sLeaser.sprites[index].scaleX = Mathf.Lerp(2f, width, DMHelper.EaseInOutCubic(charge));
                sLeaser.sprites[index].scaleY = Vector2.Distance(pos, basePos);

                //sLeaser.sprites[index].color = Color.Lerp(LaserDroneGraphics.defaultLaserColor, Color.white, burst);
                sLeaser.sprites[index].alpha = alpha;


                sLeaser.sprites[index + 1].SetPosition(pos - camPos);
                sLeaser.sprites[index + 1].scale = Mathf.Lerp(0.5f, 1f, DMHelper.EaseInOutCubic(charge));
                //sLeaser.sprites[index + 1].color = Color.Lerp(LaserDroneGraphics.defaultLaserColor, Color.white, burst);
                sLeaser.sprites[index + 1].alpha = alpha;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[index] = new FSprite("pixel", true)
                {
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    color = this.color,
                    alpha = 0f
                };
                sLeaser.sprites[index + 1] = new FSprite("DMPS_JetFlare", true)
                {
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    color = this.color,
                    alpha = 0f
                };
            }
        }
    }
}
