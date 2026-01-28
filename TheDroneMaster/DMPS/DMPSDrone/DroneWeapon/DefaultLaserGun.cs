using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSDrone.DroneWeapon
{
    internal class DefaultLaserGun : BaseDroneWeapon
    {
        public static int chargeCounter = 80, afterFireCD = 160;

        public int charge, lastCharge, chargeCD;

        public override void Update(DMPSDrone drone)
        {
            lastCharge = charge;

            bool chargingConditionMatched = ((drone.dir - drone.weaponTargetDir).magnitude < 0.06f);
            if(drone.UsingWeapon && chargeCD == 0)
            {
                foreach(var chunk in drone.AI.target.realizedCreature.bodyChunks)
                {
                    chargingConditionMatched = chargingConditionMatched && drone.room.VisualContact(drone.firstChunk.pos, chunk.pos);
                }
            }
            weaponEnable = chargingConditionMatched;

            if (chargingConditionMatched && charge < 80)
                charge++;
            else if (charge > 0)
                charge--;

            if (charge == chargeCounter)
                TryFireWeapon(drone);

            if (chargeCD > 0)
                chargeCD = 0;
        }

        public override BaseDroneWeaponGraphics InitGraphics(DMPSDroneGraphics graphics, int startSprite)
        {
            return new DefaultLaserGunGraphics(graphics, startSprite);
        }

        bool TryFireWeapon(DMPSDrone drone)
        {
            bool res = false;

            Vector2 pos = Custom.RectCollision(drone.firstChunk.pos, drone.firstChunk.pos + drone.dir * 10000f, drone.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);

            var collided = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(drone.room, drone.firstChunk.pos, pos);

            if (collided != null)
            {
                pos = drone.firstChunk.pos + drone.weaponTargetDir * Vector2.Distance(drone.firstChunk.pos, drone.room.MiddleOfTile(collided.Value));
            }

            SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(drone, drone.room, drone.firstChunk.pos, ref pos, 1f, 1, null, true);
            if (result.hitSomething)
            {
                res = true;
                (result.chunk.owner as Creature).Violence(drone.firstChunk, drone.weaponTargetDir, result.chunk, result.onAppendagePos, Creature.DamageType.Electric, 0.5f, 1f);
                FireWeapon(drone);

                drone.room.PlaySound(SoundID.Bomb_Explode, pos, 1f, 5f);
                drone.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pos, 0.5f, 3f);


                drone.room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, LaserDroneGraphics.defaultLaserColor));
            }

            return res;
        }

        public override void FireWeapon(DMPSDrone drone)
        {
            base.FireWeapon(drone);
            lastCharge = charge = 0;
            chargeCD = afterFireCD; 
        }
    }

    internal class DefaultLaserGunGraphics : BaseDroneWeaponGraphics
    {
        static int beamCount = 7;
        public override int totSprites => base.totSprites + 1 + beamCount * LaserBeam.totSprites;

        public int gunPointerIndex => startSprite + 2;

        public float burst, lastBurst;

        LaserBeam[] beams;

        public DefaultLaserGunGraphics(DMPSDroneGraphics graphics, int startSprite) : base(graphics, startSprite)
        {
            beams = new LaserBeam[beamCount];
            for(int i = 0;i< beamCount;i++)
            {
                beams[i] = new LaserBeam(this, gunPointerIndex + 1 + i * LaserBeam.totSprites, i==0);
                beams[i].RandomSpread();
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites[gunPointerIndex] = new FSprite("pixel", true);
            for (int i = 0; i < beamCount; i++)
            {
                beams[i].InitiateSprites(sLeaser, rCam);
            }
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
            sLeaser.sprites[gunPointerIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPanelAIndex]);
            for (int i = 0; i < beamCount; i++)
            {
                beams[i].AddToContainer(sLeaser, rCam, newContatiner);
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            sLeaser.sprites[gunPointerIndex].color = graphics.laserColor;
        }


        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 bodyPos, Vector2 dir, float tiltSin, float tiltCos, float rotation, float scaleXFactor, float scaleYFactor)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos, bodyPos, dir, tiltSin, tiltCos, rotation, scaleXFactor, scaleYFactor);
            sLeaser.sprites[gunPointerIndex].SetPosition(bodyPos + dir * (DMPSDroneGraphics.gunLength * scaleXFactor * tiltCos / 2f - DMPSDroneGraphics.gunWidth * scaleYFactor / 2f) - camPos);
            sLeaser.sprites[gunPointerIndex].scaleX = tiltCos * scaleXFactor * DMPSDroneGraphics.gunLength;
            sLeaser.sprites[gunPointerIndex].scaleY = DMPSDroneGraphics.gunWidth * scaleYFactor;
            sLeaser.sprites[gunPointerIndex].rotation = rotation + 90f;

            float smoothBurst = DMHelper.EaseInOutCubic(Mathf.Lerp(lastBurst, burst, timeStacker));

            for (int i = 0; i < beamCount; i++)
            {
                beams[i].DrawSprites(sLeaser, rCam, timeStacker, camPos, smoothBurst, bodyPos, dir);
            }
        }

        public override void Update(DMPSDroneGraphics graphics)
        {
            base.Update(graphics);

            lastBurst = burst;
            if (burst > 0f)
                burst = Mathf.Max(0f, burst - 1 / 10f);
        }

        public override void WeaponFired()
        {
            base.WeaponFired();
            lastBurst = burst = 1f;

            for (int i = 0; i < beamCount; i++)
            {
                beams[i].RandomSpread();
            }
        }

        class LaserBeam
        {
            public static int totSprites = 2;

            DefaultLaserGunGraphics graphics;
            int index;

            float deltaAngle;
            bool noBias;
            
            public LaserBeam(DefaultLaserGunGraphics graphics, int index, bool noBias = false)
            {
                this.graphics = graphics;
                this.index = index;
                this.noBias = noBias;
            }

            public void RandomSpread()
            {
                if(!noBias)
                    deltaAngle = Mathf.Lerp(20f, 65f, Random.value) * (Random.value > 0.5f ? 1f : -1f);
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

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, float burst, Vector2 basePos, Vector2 gunDir)
            {
                var weapon = (graphics.graphics.drone.weapon as DefaultLaserGun);
                var room = graphics.graphics.drone.room;
                if (room == null)
                    return;

                float smoothSpread = Mathf.Lerp(weapon.lastCharge, weapon.charge, timeStacker) / DefaultLaserGun.chargeCounter;
                smoothSpread = DMHelper.EaseInOutCubic(1f - smoothSpread);
                if (burst > 0f)
                    smoothSpread = 0f;

                float rotation = Custom.VecToDeg(gunDir) + smoothSpread * deltaAngle;
                Vector2 dir = Custom.DegToVec(rotation);


                Vector2 pos = Custom.RectCollision(basePos, basePos + dir * 10000f, room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                var collided = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, basePos, pos);

                if (collided != null)
                {
                    pos = basePos + dir * Vector2.Distance(basePos, room.MiddleOfTile(collided.Value));
                }

                SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(graphics.graphics.drone, room, basePos, ref pos, 2f, 1, null, true);
                if (result.hitSomething)
                    pos = result.collisionPoint;

                float flash = Mathf.Clamp01(Random.value * (1f - smoothSpread) + burst);
                float alpha = (0.2f * (1f - smoothSpread) + 0.5f * burst) * flash;

                sLeaser.sprites[index].SetPosition((pos + basePos) / 2f - camPos);
                sLeaser.sprites[index].rotation = rotation;
                sLeaser.sprites[index].scaleX = Mathf.Lerp(2f, 5f, (1f - smoothSpread) * flash) + Mathf.Lerp(0f, 3f, burst);
                sLeaser.sprites[index].scaleY = Vector2.Distance(pos, basePos);
                
                sLeaser.sprites[index].color = Color.Lerp(LaserDroneGraphics.defaultLaserColor, Color.white, burst);
                sLeaser.sprites[index].alpha = alpha;


                sLeaser.sprites[index + 1].SetPosition(pos - camPos);
                sLeaser.sprites[index + 1].scale = Mathf.Lerp(0.2f, 0.5f, (1f - smoothSpread) * flash) + Mathf.Lerp(0f, 0.5f, burst);
                sLeaser.sprites[index + 1].color = Color.Lerp(LaserDroneGraphics.defaultLaserColor, Color.white, burst);
                sLeaser.sprites[index + 1].alpha = alpha;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[index] = new FSprite("pixel", true)
                {
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    color = LaserDroneGraphics.defaultLaserColor,
                    alpha = 0f
                };
                sLeaser.sprites[index + 1] = new FSprite("DMPS_JetFlare", true)
                {
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    color = LaserDroneGraphics.defaultLaserColor,
                    alpha = 0f
                };
            }
        }
    }
}
