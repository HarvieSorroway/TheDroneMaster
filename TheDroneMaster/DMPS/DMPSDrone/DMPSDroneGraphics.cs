using RWCustom;
using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSDrone.DroneWeapon;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPSDrone
{
    internal class DMPSDroneGraphics : GraphicsModule
    {
        static bool enabledDebugGraphics = false;

        public Color droneFlameColor = new Color(0f, 0.62f, 1f);
        public Color laserColor = new Color(1f, 0.26f, 0.45f);

        public static readonly float wingWidth = 5f, droneLength = 17f, circleRad = 10f, gunLength = 15f, gunWidth = 3f;
        public static readonly float scaleFactor, scaleYFactor = 0.55f, scaleXFactor = 0.65f;
        public static readonly float maxUnfoldAngle = 60f,maxMovementRollVel = 10f, chargeRollVel = 20f;

        public static readonly int flameCount = 3, wingCount = 4, flareCount = 2;
        public static readonly int
            //laserPannelAIndex = 0,    
            //laserPannelBIndex = laserPannelAIndex + 1,
            bodySegmentIndex = 0, /*laserPannelBIndex + 1, */
            jetPannelIndex = bodySegmentIndex + 1,
            //gunPointerIndex = jetPannelIndex + 1, 
            droneFlameIndex = jetPannelIndex + 1,
            droneWingStartIndex = droneFlameIndex + flameCount,
            flareSprite = droneWingStartIndex + wingCount; 
        public int weaponSpriteStartIndex = flareSprite + flareCount;
        public int debugGraphicsIndex = flareSprite + flareCount + 1;

        public DMPSDrone drone;
        DMPSDroneDebugGraphics debugGraphics;

        public FlameTrail[] flameTrails;
        public DroneWing[] wings;

        public BaseDroneWeaponGraphics weaponGraphics;

        public Vector2 jetPos;
        bool flameSpit;

        //roll
        public int rollDir = 1;
        public float rollAngle = 0f;
        public float currentRollVel = 0f;

        //jetEnabled
        public float jet, smoothJet, lastSmoothJet;

        public float TiltSin => Mathf.Sin(45f * Mathf.Deg2Rad * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, DMPSDrone.maxVelocity, 1f, 0.2f));
        public float TiltCos => Mathf.Cos(45f * Mathf.Deg2Rad * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, DMPSDrone.maxVelocity, 1f, 0.2f));
        public float wingUnfoldAngle => maxUnfoldAngle * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, DMPSDrone.maxVelocity, 1f, 0.2f);
        public float wingUnfoldRadAngle => wingUnfoldAngle * Mathf.Deg2Rad * drone.wingStretch;

        public DMPSDroneGraphics(PhysicalObject ow) : base(ow, false)
        {
            drone = ow as DMPSDrone;
            weaponGraphics = drone.weapon.InitGraphics(this, weaponSpriteStartIndex);

            if (enabledDebugGraphics)
            {
                debugGraphicsIndex = weaponSpriteStartIndex + weaponGraphics.totSprites;
                debugGraphics = new DMPSDroneDebugGraphics(drone, debugGraphicsIndex);
            }
           

            cullRange = 1400f;

            flameTrails = new FlameTrail[3]
            {
                new FlameTrail(this, droneFlameIndex, Color.white, 4, 2.5f),
                new FlameTrail(this, droneFlameIndex + 1, Color.cyan, 8, 2f),
                new FlameTrail(this, droneFlameIndex + 2, Color.blue, 12, 1.8f)
            };

            wings = new DroneWing[4];
            for (int i = 0; i < wings.Length; i++)
            {
                wings[i] = new DroneWing(this, droneWingStartIndex + i, 360f * i / (float)wings.Length);
            }

            
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            //Plugin.Log(laserColor.ToString());

            sLeaser.sprites = new FSprite[2 + flameCount + wingCount /*+ gunGraphics.TotalSprites*/ + flareCount + weaponGraphics.totSprites + (enabledDebugGraphics ? debugGraphics.totSprite : 0)];

            sLeaser.sprites[jetPannelIndex] = new FSprite("Circle20", true);
            sLeaser.sprites[bodySegmentIndex] = new FSprite("pixel", true);
            //sLeaser.sprites[laserPannelBIndex] = new FSprite("Circle20", true);
            //sLeaser.sprites[laserPannelAIndex] = new FSprite("Circle20", true);
            //sLeaser.sprites[gunPointerIndex] = new FSprite("pixel", true);
            sLeaser.sprites[flareSprite] = new FSprite("DMPS_JetFlare", true)
            {
                color = Color.blue,
                alpha = 0,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                scale = 1.2f
            };
            sLeaser.sprites[flareSprite + 1] = new FSprite("DMPS_JetFlare", true)
            {
                color = Color.cyan,
                alpha = 0,
                shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                scale = 0.8f
            };

            for (int i = 0; i < flameCount; i++)
                flameTrails[i].InitiateSprites(sLeaser, rCam);

            for (int i = 0; i < wings.Length; i++)
            {
                wings[i].InitSprites(sLeaser, rCam);
            }

            weaponGraphics.InitiateSprites(sLeaser, rCam);
            //gunGraphics.InitSprites(sLeaser, rCam);
            if(enabledDebugGraphics) debugGraphics.InitiateSprites(sLeaser, rCam);

            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if(newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Midground");

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }

            sLeaser.sprites[jetPannelIndex].MoveBehindOtherNode(sLeaser.sprites[bodySegmentIndex]);

            sLeaser.sprites[flameTrails[0].sprite].MoveBehindOtherNode(sLeaser.sprites[jetPannelIndex]);
            sLeaser.sprites[flameTrails[1].sprite].MoveBehindOtherNode(sLeaser.sprites[flameTrails[0].sprite]);
            sLeaser.sprites[flameTrails[2].sprite].MoveBehindOtherNode(sLeaser.sprites[flameTrails[1].sprite]);
            //sLeaser.sprites[flameTrailCyan.sprite].MoveBehindOtherNode(sLeaser.sprites[flameTrailWhite.sprite]);
            //sLeaser.sprites[flameTrailBlue.sprite].MoveBehindOtherNode(sLeaser.sprites[flameTrailCyan.sprite]);

            //sLeaser.sprites[laserPannelBIndex].MoveInFrontOfOtherNode(sLeaser.sprites[bodySegmentIndex]);
            //for (int i = 0; i < wings.Length; i++)
            //{
            //    sLeaser.sprites[laserPannelBIndex].MoveInFrontOfOtherNode(sLeaser.sprites[wings[i].startIndex]);
            //}

            //sLeaser.sprites[laserPannelAIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPannelBIndex]);
            //sLeaser.sprites[gunPointerIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPannelAIndex]);

            sLeaser.sprites[flareSprite].RemoveFromContainer();
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[flareSprite]);
            sLeaser.sprites[flareSprite + 1].RemoveFromContainer();
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[flareSprite + 1]);

            weaponGraphics.AddToContainer(sLeaser, rCam, newContatiner);

            if (enabledDebugGraphics) debugGraphics.AddToContainer(sLeaser, rCam, null);
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            //sLeaser.sprites[laserPannelAIndex].color = Color.white;
            //sLeaser.sprites[laserPannelBIndex].color = laserColor;
            //sLeaser.sprites[gunPointerIndex].color = laserColor;


            sLeaser.sprites[jetPannelIndex].color = palette.blackColor;
            sLeaser.sprites[bodySegmentIndex].color = palette.blackColor;

            for (int i = 0; i < wings.Length; i++)
            {
                wings[i].ApplyPalette(sLeaser, rCam, palette);
            }

            weaponGraphics.ApplyPalette(sLeaser, rCam, palette);
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            if (culled) return;

            Vector2 bodyPos = Vector2.Lerp(owner.bodyChunks[0].lastPos, owner.bodyChunks[0].pos, timeStacker);

            Vector2 dir = Vector3.Slerp(drone.lastDir, drone.dir, timeStacker);
            float rotation = Custom.VecToDeg(dir);
            float jet = Mathf.Lerp(lastSmoothJet, smoothJet, timeStacker);

            //sLeaser.sprites[gunPointerIndex].SetPosition(bodyPos + dir * (gunLength * scaleXFactor * TiltCos / 2f - gunWidth * scaleYFactor / 2f) - camPos);
            //sLeaser.sprites[gunPointerIndex].scaleX = TiltCos * scaleXFactor * gunLength;
            //sLeaser.sprites[gunPointerIndex].scaleY = gunWidth * scaleYFactor;
            //sLeaser.sprites[gunPointerIndex].rotation = rotation + 90f;

            //sLeaser.sprites[laserPannelAIndex].SetPosition(bodyPos - camPos);
            //sLeaser.sprites[laserPannelAIndex].rotation = rotation + 90f;
            //sLeaser.sprites[laserPannelAIndex].scaleY = 0.8f * scaleYFactor;
            //sLeaser.sprites[laserPannelAIndex].scaleX = TiltSin * 0.8f * scaleXFactor;
            //sLeaser.sprites[laserPannelBIndex].SetPosition(bodyPos - camPos);
            //sLeaser.sprites[laserPannelBIndex].rotation = rotation + 90f;
            //sLeaser.sprites[laserPannelBIndex].scaleY = scaleYFactor;
            //sLeaser.sprites[laserPannelBIndex].scaleX = TiltSin * scaleXFactor;

            sLeaser.sprites[bodySegmentIndex].SetPosition(bodyPos - dir * droneLength * 0.5f * TiltCos * scaleXFactor - camPos);
            sLeaser.sprites[bodySegmentIndex].rotation = rotation + 90f;
            sLeaser.sprites[bodySegmentIndex].scaleY = circleRad * scaleYFactor * 2f;
            sLeaser.sprites[bodySegmentIndex].scaleX = droneLength * scaleXFactor * TiltCos;

            sLeaser.sprites[jetPannelIndex].SetPosition(bodyPos - dir * droneLength * TiltCos * scaleXFactor - camPos);
            sLeaser.sprites[jetPannelIndex].rotation = rotation + 90f;
            sLeaser.sprites[jetPannelIndex].scaleY = scaleYFactor;
            sLeaser.sprites[jetPannelIndex].scaleX = TiltSin * scaleXFactor;

            for (int i = 0; i < wings.Length; i++)
            {
                wings[i].DrawSprites(sLeaser, rCam, timeStacker, camPos, bodyPos, i);
            }

            jetPos = bodyPos - dir * droneLength * TiltCos * scaleXFactor;

            sLeaser.sprites[flareSprite].SetPosition(jetPos - camPos);
            sLeaser.sprites[flareSprite].alpha = Random.value * Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(0, DMPSDrone.maxVelocity, drone.mainBodyChunk.vel.magnitude)) * jet;
            sLeaser.sprites[flareSprite + 1].SetPosition(jetPos - camPos);
            sLeaser.sprites[flareSprite + 1].alpha = Random.value * Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0, DMPSDrone.maxVelocity, drone.mainBodyChunk.vel.magnitude)) * jet;

            for (int i = 0; i < flameCount; i++)
                flameTrails[i].DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPos, jet);

            weaponGraphics.DrawSprites(sLeaser, rCam, timeStacker, camPos, bodyPos, dir, TiltSin, TiltCos, rotation, scaleXFactor, scaleYFactor);
            if (enabledDebugGraphics) debugGraphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }
        public override void Update()
        {
            base.Update();
            if (culled) return;

            flameSpit = !flameSpit;
            float targetRollVel = 0f;
            if (!drone.weapon.weaponEnable)
            {
                if (Random.value < 0.01f)
                {
                    rollDir = -rollDir;
                }
                targetRollVel = rollDir * maxMovementRollVel * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, DMPSDrone.maxVelocity, 0f, 1f);
            }
            else 
                targetRollVel = rollDir * chargeRollVel;

            lastSmoothJet = smoothJet;
            if (drone.disableJet && jet > 0)
            {
                jet = Mathf.Max(0, jet - 1 / 40f);
                smoothJet = DMHelper.EaseInOutCubic(jet);
            }
            else if(!drone.disableJet && jet < 1)
            {
                jet = Mathf.Min(1, jet + 1 / 40f);
                smoothJet = DMHelper.EaseInOutCubic(jet);
            }

            currentRollVel = Mathf.Lerp(currentRollVel, targetRollVel, 0.1f);
            rollAngle += currentRollVel;

            for (int i = 0; i < flameCount; i++)
                flameTrails[i].Update();

            weaponGraphics.Update(this);
        }

        public override void Reset()
        {
            base.Reset();
            Vector2 dir = drone.dir;
            jetPos = drone.DangerPos - dir * droneLength * TiltCos * scaleXFactor;

            for(int i = 0;i < flameCount;i++)
                flameTrails[i].Reset();
        }
    }

    internal class DroneWing
    {
        public DMPSDroneGraphics droneGraphics;
        public int startIndex;
        public float rotationBias;

        public float lastRollAngle;

        //0 = A, 1 = B, 2 = C, 3 = D
        public Vector2[] lastPostions = new Vector2[4];
        public Vector2[] positions = new Vector2[4];

        public float thisRollAngle => droneGraphics.rollAngle + rotationBias;
        public float wingWidth => DMPSDroneGraphics.wingWidth;
        public float wingLength => DMPSDroneGraphics.droneLength * DMPSDroneGraphics.scaleXFactor;
        public float rad => DMPSDroneGraphics.circleRad * DMPSDroneGraphics.scaleYFactor * 0.8f;
        public float unfoldAngle => droneGraphics.wingUnfoldRadAngle;

        public DroneWing(DMPSDroneGraphics droneGraphics, int startIndex, float rotationBias = 0f)
        {
            this.droneGraphics = droneGraphics;
            this.rotationBias = rotationBias;
            this.startIndex = startIndex;

            lastRollAngle = thisRollAngle;

            UpdatePos(1f);
            for (int i = 0; i < 4; i++)
            {
                lastPostions[i] = positions[i];
            }
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[startIndex] = new CustomFSprite("pixel");
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = 0; i < 4; i++)
            {
                (sLeaser.sprites[startIndex] as CustomFSprite).verticeColors[i] = palette.blackColor;
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 bodyPos, int index)
        {
            UpdatePos(timeStacker);
            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = Vector2.Lerp(lastPostions[i], positions[i], timeStacker);
                (sLeaser.sprites[startIndex] as CustomFSprite).MoveVertice(i, pos + bodyPos - camPos);
            }
        }

        public void UpdatePos(float timeStacker)
        {
            for (int i = 0; i < 4; i++)
            {
                lastPostions[i] = positions[i];
            }

            float rollAngle = Mathf.Lerp(lastRollAngle, thisRollAngle, timeStacker);
            lastRollAngle = rollAngle;


            Vector2 dir = Custom.DegToVec(rollAngle);
            Vector2 droneDir = Vector3.Slerp(droneGraphics.drone.lastDir, droneGraphics.drone.dir, timeStacker);
            Vector2 perpendicularDir = Custom.PerpendicularVector(dir);

            Vector2 posM = dir * rad;
            Vector2 posN = dir * (rad + wingLength * Mathf.Sin(unfoldAngle));

            Vector2 foldWingVec = new Vector2(-wingLength * droneGraphics.TiltCos * Mathf.Cos(unfoldAngle), 0f);
            foldWingVec = Custom.rotateVectorDeg(foldWingVec, Custom.VecToDeg(droneDir) - 90f);

            positions[0] = posM + perpendicularDir * wingWidth / 2f;
            positions[1] = posM - perpendicularDir * wingWidth / 2f;
            positions[2] = posN - perpendicularDir * wingWidth / 2f;
            positions[3] = posN + perpendicularDir * wingWidth / 2f;

            for (int i = 0; i < 4; i++)
            {
                positions[i].x *= droneGraphics.TiltSin;

                positions[i] = Custom.rotateVectorDeg(positions[i], Custom.VecToDeg(droneDir) - 90f);
                if (i == 2 || i == 3) positions[i] += foldWingVec;
            }
        }
    }

    internal class FlameTrail
    {
        public int maxTrailLength;

        public DMPSDroneGraphics owner;

        public List<Vector2> positionsList;
        public List<Color> colorsList;

        public int sprite;
        public int savPoss;
        public float powFactor;
        public Color color;
        public FlameTrail(DMPSDroneGraphics owner, int sprite, Color color, int maxTrailLength = 20, float powFactor = 1.5f)
        {
            this.sprite = sprite;
            this.owner = owner;
            this.savPoss = maxTrailLength;
            this.color = color;
            this.maxTrailLength = maxTrailLength;
            this.powFactor = powFactor;
            this.Reset();
        }

        public void Reset()
        {
            positionsList = new List<Vector2>
            {
                owner.jetPos
            };
            colorsList = new List<Color>
            {
                color
            };
        }

        public Vector2 GetSmoothPos(int i, float timeStacker)
        {
            return Vector2.Lerp(this.GetPos(i + 1), this.GetPos(i), timeStacker);
        }

        public Vector2 GetPos(int i)
        {
            return this.positionsList[Custom.IntClamp(i, 0, this.positionsList.Count - 1)];
        }

        public Color GetCol(int i)
        {
            var col = colorsList[Custom.IntClamp(i, 0, this.colorsList.Count - 1)];
            return col;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            int segments = savPoss - 1;
            bool pointyTip = false;
            bool customColor = true;
            TriangleMesh.Triangle[] array = new TriangleMesh.Triangle[(segments - 1) * 4 + (pointyTip ? 1 : 2)];
            for (int i = 0; i < segments - 1; i++)
            {
                int num = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    array[num + j] = new TriangleMesh.Triangle(num + j, num + j + 1, num + j + 2);
                }
            }
            array[(segments - 1) * 4] = new TriangleMesh.Triangle((segments - 1) * 4, (segments - 1) * 4 + 1, (segments - 1) * 4 + 2);
            if (!pointyTip)
            {
                array[(segments - 1) * 4 + 1] = new TriangleMesh.Triangle((segments - 1) * 4 + 1, (segments - 1) * 4 + 2, (segments - 1) * 4 + 3);
            }
            TriangleMesh triangleMesh = new TriangleMesh("pixel", array, customColor, true);
            triangleMesh.alpha = 0.8f;

            sLeaser.sprites[this.sprite] = triangleMesh;
        }

        public void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 jetPos, float jet)
        {
            Vector2 vector = jetPos;
            float num = DMPSDroneGraphics.circleRad * DMPSDroneGraphics.scaleYFactor * 0.5f;

            if (jet == 0f)
            {
                sLeaser.sprites[sprite].isVisible = false;
                return;
            }
            else
                sLeaser.sprites[sprite].isVisible = true;

            for (int i = 0; i < savPoss - 1; i++)
            {
                Vector2 smoothPos = Vector2.Lerp(jetPos, GetSmoothPos(i, timeStacker), jet);
                Vector2 smoothPos2 = Vector2.Lerp(jetPos, GetSmoothPos(i + 1, timeStacker), jet);
                Vector2 dir = (vector - smoothPos).normalized;
                Vector2 perDir = Custom.PerpendicularVector(dir);
                dir *= Vector2.Distance(vector, smoothPos2) / 5f;
                float width = Mathf.Pow(Custom.LerpMap(i, 0, savPoss - 1, 1f, 0f), powFactor);
                (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(i * 4, vector - perDir * num * width - dir - camPos);
                (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector + perDir * num * width - dir - camPos);
                (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(i * 4 + 2, smoothPos - perDir * num * width + dir - camPos);
                (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(i * 4 + 3, smoothPos + perDir * num * width + dir - camPos);
                vector = smoothPos;
            }
            for (int j = 0; j < (sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors.Length; j++)
            {
                float num2 = (float)j / (float)((sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors.Length - 1);
                (sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors[j] = GetCol(j);
            }
        }

        public void SetVisible(RoomCamera.SpriteLeaser sLeaser, bool visible)
        {
            sLeaser.sprites[sprite].isVisible = visible;
        }

        public void Update()
        {
            float vel = owner.drone.mainBodyChunk.vel.magnitude;
            Vector2 dir = owner.drone.dir;

            positionsList.Insert(0, owner.jetPos);
            if (positionsList.Count > maxTrailLength)
            {
                positionsList.RemoveAt(maxTrailLength);
            }
            colorsList.Insert(0, color);
            if (colorsList.Count > maxTrailLength)
            {
                colorsList.RemoveAt(maxTrailLength);
            }
            for (int i = 0; i < positionsList.Count; i++)
            {
                positionsList[i] -= dir * 1.5f * 2f * Custom.LerpMap(vel, 0f, 1.5f, 1f, 0f);
            }
        }
    }
}
