using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DreamComponent.OracleHooks
{
    public class TestSSOrcale : CustomOracle
    {
        public static Oracle.OracleID DMDOracle = new Oracle.OracleID("DMD", true);
        public override string LoadRoom => "DMD_AI";
        public override Oracle.OracleID OracleID => Oracle.OracleID.SS;
        public override Oracle.OracleID InheritOracleID => Oracle.OracleID.SS;

        public TestSSOrcale()
        {
            gravity = 0f;
            startPos = new Vector2(350f, 350f);
        }

        public override void LoadBehaviourAndSurroundings(ref Oracle oracle, Room room)
        {
            oracle.oracleBehavior = new SSOracleBehavior(oracle);
            oracle.myScreen = new OracleProjectionScreen(room, oracle.oracleBehavior);
            room.AddObject(oracle.myScreen);
            oracle.marbles = new List<PebblesPearl>();
            oracle.SetUpMarbles();
            room.gravity = 0f;
            for (int n = 0; n < room.updateList.Count; n++)
            {
                if (room.updateList[n] is AntiGravity)
                {
                    (room.updateList[n] as AntiGravity).active = false;
                    break;
                }
            }
            oracle.arm = new Oracle.OracleArm(oracle);
            Plugin.Log("Successfully load behaviours and surroundings!");
        }

        public override OracleGraphics InitCustomOracleGraphic(PhysicalObject ow)
        {
            return new TestSSOracleGraphics(ow);
        }
    }
    public class TestSSOracleGraphics : CustomOracleGraphic
    {
        public GownCover cover;
        public int coverSprite;

        public TestSSOracleGraphics(PhysicalObject ow) : base(ow)
        {
            callBaseApplyPalette = false;
            callBaseInitiateSprites = false;

            Random.State state = Random.state;
            Random.InitState(56);
            this.totalSprites = 0;
            this.armJointGraphics = new ArmJointGraphics[this.oracle.arm.joints.Length];

            for (int i = 0; i < this.oracle.arm.joints.Length; i++)
            {
                this.armJointGraphics[i] = new ArmJointGraphics(this, this.oracle.arm.joints[i], this.totalSprites);
                this.totalSprites += this.armJointGraphics[i].totalSprites;
            }


            this.firstUmbilicalSprite = this.totalSprites;
            this.umbCord = new UbilicalCord(this, this.totalSprites);
            this.totalSprites += this.umbCord.totalSprites;


            this.firstBodyChunkSprite = this.totalSprites;
            this.totalSprites += 2;
            this.neckSprite = this.totalSprites;
            this.totalSprites++;
            this.firstFootSprite = this.totalSprites;
            this.totalSprites += 4;


            this.halo = new Halo(this, this.totalSprites);
            this.totalSprites += this.halo.totalSprites;
            this.gown = new Gown(this);
            this.robeSprite = this.totalSprites;
            this.totalSprites++;


            this.firstHandSprite = this.totalSprites;
            this.totalSprites += 4;
            this.head = new GenericBodyPart(this, 5f, 0.5f, 0.995f, this.oracle.firstChunk);
            this.firstHeadSprite = this.totalSprites;
            this.totalSprites += 10;
            this.fadeSprite = this.totalSprites;
            this.totalSprites++;


            this.killSprite = this.totalSprites;
            this.totalSprites++;

            this.hands = new GenericBodyPart[2];

            for (int j = 0; j < 2; j++)
            {
                this.hands[j] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, this.oracle.firstChunk);
            }
            this.feet = new GenericBodyPart[2];
            for (int k = 0; k < 2; k++)
            {
                this.feet[k] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, this.oracle.firstChunk);
            }
            this.knees = new Vector2[2, 2];
            for (int l = 0; l < 2; l++)
            {
                for (int m = 0; m < 2; m++)
                {
                    this.knees[l, m] = this.oracle.firstChunk.pos;
                }
            }
            this.firstArmBaseSprite = this.totalSprites;
            this.armBase = new ArmBase(this, this.firstArmBaseSprite);
            this.totalSprites += this.armBase.totalSprites;

            cover = new GownCover(this);
            coverSprite = this.totalSprites;
            totalSprites++;

            this.voiceFreqSamples = new float[64];
            Random.state = state;
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);

            this.SLArmBaseColA = new Color(0.52156866f, 0.52156866f, 0.5137255f);
            this.SLArmHighLightColA = new Color(0.5686275f, 0.5686275f, 0.54901963f);
            this.SLArmBaseColB = palette.texture.GetPixel(5, 1);
            this.SLArmHighLightColB = palette.texture.GetPixel(5, 2);

            for (int i = 0; i < this.armJointGraphics.Length; i++)
            {
                this.armJointGraphics[i].ApplyPalette(sLeaser, rCam, palette);
                armJointGraphics[i].metalColor = palette.blackColor;
            }
            Color color = new Color(52f / 255f, 61f / 255f, 83f / 255f);

            for (int j = 0; j < base.owner.bodyChunks.Length; j++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + j].color = color;
            }
            sLeaser.sprites[this.neckSprite].color = color;
            sLeaser.sprites[this.HeadSprite].color = color;
            sLeaser.sprites[this.ChinSprite].color = color;

            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[this.EyeSprite(k)].color = new Color(255f / 255f, 67f / 255f, 115f / 255f);
            }

            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[this.PhoneSprite(k, 0)].color = new Color(72f / 255f, 83f / 255f, 107f / 255f);
                sLeaser.sprites[this.PhoneSprite(k, 1)].color = new Color(72f / 255f, 83f / 255f, 107f / 255f);
                sLeaser.sprites[this.PhoneSprite(k, 2)].color = new Color(72f / 255f, 83f / 255f, 107f / 255f);


                sLeaser.sprites[this.HandSprite(k, 0)].color = color;
                if (this.gown != null)
                {
                    for (int l = 0; l < 7; l++)
                    {
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4] = this.cover.Color(l + 6);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 1] = this.cover.Color(l + 6);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 2] = this.cover.Color(l + 6);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 3] = this.cover.Color(l + 6);
                    }
                }
                else
                {
                    sLeaser.sprites[this.HandSprite(k, 1)].color = color;
                }
                sLeaser.sprites[this.FootSprite(k, 0)].color = color;
                sLeaser.sprites[this.FootSprite(k, 1)].color = color;
            }
            if (this.umbCord != null)
            {
                this.umbCord.ApplyPalette(sLeaser, rCam, palette);
                sLeaser.sprites[this.firstUmbilicalSprite].color = palette.blackColor;
            }
            else if (this.discUmbCord != null)
            {
                this.discUmbCord.ApplyPalette(sLeaser, rCam, palette);
            }
            if (this.armBase != null)
            {
                this.armBase.ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.totalSprites];
            for (int i = 0; i < base.owner.bodyChunks.Length; i++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + i] = new FSprite("Circle20", true);
                sLeaser.sprites[this.firstBodyChunkSprite + i].scale = base.owner.bodyChunks[i].rad / 10f;
                sLeaser.sprites[this.firstBodyChunkSprite + i].color = new Color(1f, (i == 0) ? 0.5f : 0f, (i == 0) ? 0.5f : 0f);
            }

            for (int j = 0; j < this.armJointGraphics.Length; j++)
            {
                this.armJointGraphics[j].InitiateSprites(sLeaser, rCam);
            }

            if (this.gown != null)
            {
                this.gown.InitiateSprite(this.robeSprite, sLeaser, rCam);
            }

            if (this.halo != null)
            {
                this.halo.InitiateSprites(sLeaser, rCam);
            }

            if (this.armBase != null)
            {
                this.armBase.InitiateSprites(sLeaser, rCam);
            }
            sLeaser.sprites[this.neckSprite] = new FSprite("pixel", true);
            sLeaser.sprites[this.neckSprite].scaleX = 3f;
            sLeaser.sprites[this.neckSprite].anchorY = 0f;
            sLeaser.sprites[this.HeadSprite] = new FSprite("Circle20", true);
            sLeaser.sprites[this.ChinSprite] = new FSprite("Circle20", true);
            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[this.EyeSprite(k)] = new FSprite("pixel", true);

                sLeaser.sprites[this.PhoneSprite(k, 0)] = new FSprite("Circle20", true);
                sLeaser.sprites[this.PhoneSprite(k, 1)] = new FSprite("Circle20", true);
                sLeaser.sprites[this.PhoneSprite(k, 2)] = new FSprite("LizardScaleA1", true);
                sLeaser.sprites[this.PhoneSprite(k, 2)].anchorY = 0f;
                sLeaser.sprites[this.PhoneSprite(k, 2)].scaleY = 0.8f;
                sLeaser.sprites[this.PhoneSprite(k, 2)].scaleX = ((k == 0) ? -1f : 1f) * 0.75f;

                sLeaser.sprites[this.HandSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                sLeaser.sprites[this.HandSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                sLeaser.sprites[this.FootSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                sLeaser.sprites[this.FootSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
            }

            if (this.umbCord != null)
            {
                this.umbCord.InitiateSprites(sLeaser, rCam);
            }
            else if (this.discUmbCord != null)
            {
                this.discUmbCord.InitiateSprites(sLeaser, rCam);
            }

            sLeaser.sprites[this.HeadSprite].scaleX = this.head.rad / 9f;
            sLeaser.sprites[this.HeadSprite].scaleY = this.head.rad / 11f;
            sLeaser.sprites[this.ChinSprite].scale = this.head.rad / 15f;
            sLeaser.sprites[this.fadeSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.fadeSprite].scale = 12.5f;
            sLeaser.sprites[this.fadeSprite].color = new Color(255f / 255f, 67f / 255f, 115f / 255f);

            sLeaser.sprites[this.fadeSprite].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            sLeaser.sprites[this.fadeSprite].alpha = 0.5f;

            sLeaser.sprites[this.killSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.killSprite].shader = rCam.game.rainWorld.Shaders["FlatLight"];

            cover.InitiateSprites(coverSprite, sLeaser, rCam);

            base.InitiateSprites(sLeaser, rCam);
            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[coverSprite]);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            cover.DrawSprites(coverSprite, sLeaser, rCam, timeStacker, camPos);
        }

        public override void Update()
        {
            base.Update();
            cover.Update();
        }

        public override Color ArmJoint_HighLightColor(ArmJointGraphics armJointGraphics, Vector2 pos)
        {
            return new Color(255f / 256f, 213f / 256f, 231f / 256f);
        }

        public override Color ArmJoint_BaseColor(ArmJointGraphics armJointGraphics, Vector2 pos)
        {
            return new Color(210f / 256f, 139f / 256f, 170f / 256f);
        }

        public override Color UbilicalCord_WireCol_1(UbilicalCord ubilicalCord)
        {
            return new Color(255f / 255f, 67f / 255f, 115f / 255f);
        }

        public override Color UbilicalCord_WireCol_2(UbilicalCord ubilicalCord)
        {
            return new Color(111f / 255f, 28f / 255f, 213f / 255f);
        }

        public override Color Gown_Color(Gown gown, float f)
        {
            return new Color(237f / 255f, 236f / 255f, 248f / 255f);
        }

        public class GownCover
        {
            public OracleGraphics owner;

            public int divs = 11;

            public Vector2[] collarPos;
            public Vector2[] leftSleevePos;
            public Vector2[] rightSleevePos;

            public Vector2[] midColPos;

            public GownCover(OracleGraphics owner)
            {
                this.owner = owner;

                collarPos = new Vector2[divs];
                leftSleevePos = new Vector2[10];
                rightSleevePos = new Vector2[10];

                midColPos = new Vector2[divs];
            }

            public Color Color(int y)
            {
                if (y < 2) return new Color(237f / 255f, 236f / 255f, 248f / 255f);
                else if (y < 9) return new Color(87f / 255f, 94f / 255f, 113f / 255f);
                else if (y < 11) return new Color(255f / 255f, 67f / 255f, 115f / 255f);
                else return new Color(237f / 255f, 236f / 255f, 248f / 255f);
            }

            public void InitiateSprites(int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[sprite] = TriangleMesh.MakeGridMesh("Futile_White", divs - 1);

                for (int x = 0; x < this.divs; x++)
                {
                    for (int y = 0; y < this.divs; y++)
                    {
                        (sLeaser.sprites[sprite] as TriangleMesh).verticeColors[y * this.divs + x] = Color(y);
                    }
                }
            }

            public void Update()
            {
                for (int x = 0; x < divs; x++)
                {
                    collarPos[x] = owner.gown.clothPoints[x, 0, 0];
                }

                for (int y = 0; y < divs; y++)
                {
                    Vector2 delta = owner.gown.clothPoints[5, y, 0] - owner.gown.clothPoints[5, 0, 0];
                    midColPos[y] = owner.gown.clothPoints[5, 0, 0] + delta / 2f;
                }
            }

            public void DrawSprites(int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 smoothedBodyPos = Vector2.Lerp(owner.owner.firstChunk.lastPos, owner.owner.firstChunk.pos, timeStacker);
                Vector2 bodyDir = Custom.DirVec(Vector2.Lerp(owner.owner.bodyChunks[1].lastPos, owner.owner.bodyChunks[1].pos, timeStacker), smoothedBodyPos);
                Vector2 perpBodyDir = Custom.PerpendicularVector(bodyDir);

                for (int k = 0; k < 2; k++)
                {
                    Vector2 smoothedHandPos = Vector2.Lerp(owner.hands[k].lastPos, owner.hands[k].pos, timeStacker);
                    Vector2 shoulderPos = smoothedBodyPos + perpBodyDir * 4f * ((k == 1) ? -1f : 1f);
                    Vector2 cB = smoothedHandPos + Custom.DirVec(smoothedHandPos, shoulderPos) * 3f + bodyDir;
                    Vector2 cA = shoulderPos + perpBodyDir * 5f * ((k == 1) ? -1f : 1f);

                    Vector2 vector14 = shoulderPos - perpBodyDir * 2f * ((k == 1) ? -1f : 1f);
                    float sleeveWidth = 4f;

                    for (int m = 0; m < 5; m++)
                    {

                        float f = (float)m / 6f;
                        Vector2 sleevePosOnBezier = Custom.Bezier(shoulderPos, cA, smoothedHandPos, cB, f);
                        Vector2 vector16 = Custom.DirVec(vector14, sleevePosOnBezier);
                        Vector2 vector17 = Custom.PerpendicularVector(vector16) * ((k == 0) ? -1f : 1f);
                        float num6 = Vector2.Distance(vector14, sleevePosOnBezier);

                        Vector2 posA = sleevePosOnBezier - vector16 * num6 * 0.3f + vector17 * sleeveWidth - camPos;
                        Vector2 posB = sleevePosOnBezier + vector17 * sleeveWidth - camPos;

                        if (k == 0)
                        {
                            leftSleevePos[m * 2] = posA;
                            leftSleevePos[m * 2 + 1] = posB;
                        }
                        else
                        {
                            rightSleevePos[(m * 2)] = posA;
                            rightSleevePos[(m * 2) + 1] = posB;
                        }
                        vector14 = sleevePosOnBezier;
                    }
                }

                for (int x = 0; x < this.divs; x++)//draw collar
                {
                    (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(0 * divs + x, collarPos[x] - camPos);
                }

                for (int y = 1; y < divs; y++)//draw left and right
                {
                    (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(y * divs + 0, leftSleevePos[y - 1]);
                    (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(y * divs + divs - 1, rightSleevePos[y - 1]);
                }


                for (int y = 1; y < this.divs; y++)//draw mid
                {
                    (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(y * divs + 5, midColPos[y] - camPos);
                }

                for (int x = 1; x < 5; x++)
                {
                    for (int y = 1; y < this.divs; y++)
                    {
                        Vector2 left = (sLeaser.sprites[sprite] as TriangleMesh).vertices[y * divs + 0];
                        Vector2 mid = (sLeaser.sprites[sprite] as TriangleMesh).vertices[y * divs + 5];
                        float t = Mathf.InverseLerp(0f, 5f, x);

                        (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(y * divs + x, Vector2.Lerp(left, mid, t));
                    }
                }

                for (int x = 6; x < divs - 1; x++)
                {
                    for (int y = 1; y < this.divs; y++)
                    {
                        Vector2 right = (sLeaser.sprites[sprite] as TriangleMesh).vertices[y * divs + divs - 1];
                        Vector2 mid = (sLeaser.sprites[sprite] as TriangleMesh).vertices[y * divs + 5];
                        float t = Mathf.InverseLerp(5, divs, x);

                        (sLeaser.sprites[sprite] as TriangleMesh).MoveVertice(y * divs + x, Vector2.Lerp(mid, right, t));
                    }
                }
            }
        }
    }

    public class MIFOracleBehaviour : OracleBehavior
    {
        private Vector2 lastPos;

        private Vector2 nextPos;

        private Vector2 lastPosHandle;

        private Vector2 nextPosHandle;

        private Vector2 currentGetTo;

        public MIFOracleBehaviour(Oracle oracle) : base(oracle) 
        {
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            //this.currentGetTo = Custom.Bezier(this.lastPos, this.ClampVectorInRoom(this.lastPos + this.lastPosHandle), this.nextPos, this.ClampVectorInRoom(this.nextPos + this.nextPosHandle), this.pathProgression);
        }

        public Vector2 ClampVectorInRoom(Vector2 v)
        {
            Vector2 result = v;
            result.x = Mathf.Clamp(result.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
            result.y = Mathf.Clamp(result.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
            return result;
        }
    }
}
