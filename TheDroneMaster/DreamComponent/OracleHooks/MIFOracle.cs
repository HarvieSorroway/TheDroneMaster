using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.GameHooks;
using UnityEngine;
using static TheDroneMaster.DreamComponent.OracleHooks.CustomOracleBehaviour;
using static TheDroneMaster.DreamComponent.OracleHooks.CustomOracleBehaviour.CustomOracleConversation;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DreamComponent.OracleHooks
{
    public class MIFOracle : CustomOracle
    {
        public static Oracle.OracleID DMDOracle = new Oracle.OracleID("DMD", true);
        public override string LoadRoom => "DMD_AI";
        public override Oracle.OracleID OracleID => DMDOracle;
        public override Oracle.OracleID InheritOracleID => Oracle.OracleID.SS;

        public MIFOracle()
        {
            gravity = 0f;
            startPos = new Vector2(350f, 350f);
        }

        public override void LoadBehaviourAndSurroundings(ref Oracle oracle, Room room)
        {
            oracle.oracleBehavior = new MIFOracleBehaviour(oracle);

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
            return new MIFOracleGraphics(ow);
        }
    }
    public class MIFOracleGraphics : CustomOracleGraphic
    {
        public GownCover cover;
        public int coverSprite;

        public MIFOracleGraphics(PhysicalObject ow) : base(ow)
        {
            callBaseApplyPalette = false;
            callBaseInitiateSprites = false;

            Random.State state = Random.state;
            Random.InitState(56);
            totalSprites = 0;
            armJointGraphics = new ArmJointGraphics[oracle.arm.joints.Length];

            for (int i = 0; i < oracle.arm.joints.Length; i++)
            {
                armJointGraphics[i] = new ArmJointGraphics(this, oracle.arm.joints[i], totalSprites);
                totalSprites += armJointGraphics[i].totalSprites;
            }


            firstUmbilicalSprite = totalSprites;
            umbCord = new UbilicalCord(this, totalSprites);
            totalSprites += umbCord.totalSprites;


            firstBodyChunkSprite = totalSprites;
            totalSprites += 2;
            neckSprite = totalSprites;
            totalSprites++;
            firstFootSprite = totalSprites;
            totalSprites += 4;


            halo = new Halo(this, totalSprites);
            totalSprites += halo.totalSprites;
            gown = new Gown(this);
            robeSprite = totalSprites;
            totalSprites++;


            firstHandSprite = totalSprites;
            totalSprites += 4;
            head = new GenericBodyPart(this, 5f, 0.5f, 0.995f, oracle.firstChunk);
            firstHeadSprite = totalSprites;
            totalSprites += 10;
            fadeSprite = totalSprites;
            totalSprites++;

            //killSprite = totalSprites;
            //totalSprites++;

            hands = new GenericBodyPart[2];

            for (int j = 0; j < 2; j++)
            {
                hands[j] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, oracle.firstChunk);
            }
            feet = new GenericBodyPart[2];
            for (int k = 0; k < 2; k++)
            {
                feet[k] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, oracle.firstChunk);
            }
            knees = new Vector2[2, 2];
            for (int l = 0; l < 2; l++)
            {
                for (int m = 0; m < 2; m++)
                {
                    knees[l, m] = oracle.firstChunk.pos;
                }
            }
            firstArmBaseSprite = totalSprites;
            armBase = new ArmBase(this, firstArmBaseSprite);
            totalSprites += armBase.totalSprites;

            cover = new GownCover(this);
            coverSprite = totalSprites;
            totalSprites++;

            voiceFreqSamples = new float[64];
            Random.state = state;
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);

            SLArmBaseColA = new Color(0.52156866f, 0.52156866f, 0.5137255f);
            SLArmHighLightColA = new Color(0.5686275f, 0.5686275f, 0.54901963f);
            SLArmBaseColB = palette.texture.GetPixel(5, 1);
            SLArmHighLightColB = palette.texture.GetPixel(5, 2);

            for (int i = 0; i < armJointGraphics.Length; i++)
            {
                armJointGraphics[i].ApplyPalette(sLeaser, rCam, palette);
                armJointGraphics[i].metalColor = palette.blackColor;
            }
            Color color = new Color(52f / 255f, 61f / 255f, 83f / 255f);

            for (int j = 0; j < base.owner.bodyChunks.Length; j++)
            {
                sLeaser.sprites[firstBodyChunkSprite + j].color = color;
            }
            sLeaser.sprites[neckSprite].color = color;
            sLeaser.sprites[HeadSprite].color = color;
            sLeaser.sprites[ChinSprite].color = color;

            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[EyeSprite(k)].color = new Color(255f / 255f, 67f / 255f, 115f / 255f);
            }

            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[PhoneSprite(k, 0)].color = new Color(72f / 255f, 83f / 255f, 107f / 255f);
                sLeaser.sprites[PhoneSprite(k, 1)].color = new Color(72f / 255f, 83f / 255f, 107f / 255f);
                sLeaser.sprites[PhoneSprite(k, 2)].color = new Color(72f / 255f, 83f / 255f, 107f / 255f);


                sLeaser.sprites[HandSprite(k, 0)].color = color;
                if (gown != null)
                {
                    for (int l = 0; l < 7; l++)
                    {
                        (sLeaser.sprites[HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4] = cover.Color(l + 6);
                        (sLeaser.sprites[HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 1] = cover.Color(l + 6);
                        (sLeaser.sprites[HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 2] = cover.Color(l + 6);
                        (sLeaser.sprites[HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 3] = cover.Color(l + 6);
                    }
                }
                else
                {
                    sLeaser.sprites[HandSprite(k, 1)].color = color;
                }
                sLeaser.sprites[FootSprite(k, 0)].color = color;
                sLeaser.sprites[FootSprite(k, 1)].color = color;
            }
            if (umbCord != null)
            {
                umbCord.ApplyPalette(sLeaser, rCam, palette);
                sLeaser.sprites[firstUmbilicalSprite].color = palette.blackColor;
            }
            else if (discUmbCord != null)
            {
                discUmbCord.ApplyPalette(sLeaser, rCam, palette);
            }
            if (armBase != null)
            {
                armBase.ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[totalSprites];
            for (int i = 0; i < base.owner.bodyChunks.Length; i++)
            {
                sLeaser.sprites[firstBodyChunkSprite + i] = new FSprite("Circle20", true);
                sLeaser.sprites[firstBodyChunkSprite + i].scale = base.owner.bodyChunks[i].rad / 10f;
                sLeaser.sprites[firstBodyChunkSprite + i].color = new Color(1f, (i == 0) ? 0.5f : 0f, (i == 0) ? 0.5f : 0f);
            }

            for (int j = 0; j < armJointGraphics.Length; j++)
            {
                armJointGraphics[j].InitiateSprites(sLeaser, rCam);
            }

            if (gown != null)
            {
                gown.InitiateSprite(robeSprite, sLeaser, rCam);
            }

            if (halo != null)
            {
                halo.InitiateSprites(sLeaser, rCam);
            }

            if (armBase != null)
            {
                armBase.InitiateSprites(sLeaser, rCam);
            }
            sLeaser.sprites[neckSprite] = new FSprite("pixel", true);
            sLeaser.sprites[neckSprite].scaleX = 3f;
            sLeaser.sprites[neckSprite].anchorY = 0f;
            sLeaser.sprites[HeadSprite] = new FSprite("Circle20", true);
            sLeaser.sprites[ChinSprite] = new FSprite("Circle20", true);
            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[EyeSprite(k)] = new FSprite("pixel", true);

                sLeaser.sprites[PhoneSprite(k, 0)] = new FSprite("Circle20", true);
                sLeaser.sprites[PhoneSprite(k, 1)] = new FSprite("Circle20", true);
                sLeaser.sprites[PhoneSprite(k, 2)] = new FSprite("LizardScaleA1", true);
                sLeaser.sprites[PhoneSprite(k, 2)].anchorY = 0f;
                sLeaser.sprites[PhoneSprite(k, 2)].scaleY = 0.8f;
                sLeaser.sprites[PhoneSprite(k, 2)].scaleX = ((k == 0) ? -1f : 1f) * 0.75f;

                sLeaser.sprites[HandSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                sLeaser.sprites[HandSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                sLeaser.sprites[FootSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                sLeaser.sprites[FootSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
            }

            if (umbCord != null)
            {
                umbCord.InitiateSprites(sLeaser, rCam);
            }
            else if (discUmbCord != null)
            {
                discUmbCord.InitiateSprites(sLeaser, rCam);
            }

            sLeaser.sprites[HeadSprite].scaleX = head.rad / 9f;
            sLeaser.sprites[HeadSprite].scaleY = head.rad / 11f;
            sLeaser.sprites[ChinSprite].scale = head.rad / 15f;
            sLeaser.sprites[fadeSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[fadeSprite].scale = 12.5f;
            sLeaser.sprites[fadeSprite].color = new Color(255f / 255f, 67f / 255f, 115f / 255f);

            sLeaser.sprites[fadeSprite].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            sLeaser.sprites[fadeSprite].alpha = 0.5f;

            sLeaser.sprites[killSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[killSprite].shader = rCam.game.rainWorld.Shaders["FlatLight"];

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

    public class MIFOracleBehaviour : CustomOracleBehaviour
    {
        public static CustomAction MeetDroneMaster_Init = new CustomAction("MeeetDroneMaster_Init", true);
        public static CustomAction MeetDroneMaster_DreamTalk0 = new CustomAction("MeeetDroneMaster_DreamTalk0", true);
        public static CustomAction MeetDroneMaster_DreamTalk1 = new CustomAction("MeeetDroneMaster_DreamTalk1", true);
        public static CustomAction MeetDroneMaster_DreamTalk2 = new CustomAction("MeeetDroneMaster_DreamTalk2", true);

        public static Conversation.ID DroneMaster_DreamTalk0 = new Conversation.ID("DroneMaster_DreamTalk0", true);
        public static Conversation.ID DroneMaster_DreamTalk1 = new Conversation.ID("DroneMaster_DreamTalk1", true);
        public static Conversation.ID DroneMaster_DreamTalk2 = new Conversation.ID("DroneMaster_DreamTalk2", true);

        public static CustomSubBehaviour.CustomSubBehaviourID MeetDroneMaster = new CustomSubBehaviour.CustomSubBehaviourID("MeetDroneMaster", true);

        public int ConversationHad = 0;

        public override int GetWorkingPalette => 79;
        public override Vector2 GetToDir => Vector2.up;

        public MIFOracleBehaviour(Oracle oracle) : base(oracle) 
        {
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
        }

        public override void SeePlayer()
        {
            base.SeePlayer();
            Plugin.Log("Oracle see player");

            if(ConversationHad == 0)
            {
                ConversationHad++;
                NewAction(MeetDroneMaster_Init);
            }
        }


        public override void NewAction(CustomAction nextAction)
        {
            Plugin.Log(string.Concat(new string[]
            {
                "new action: ",
                nextAction.ToString(),
                " (from ",
                action.ToString(),
                ")"
            }));

            if (nextAction == action) return;
            CustomSubBehaviour.CustomSubBehaviourID customSubBehaviourID = null;

            if (nextAction == MeetDroneMaster_Init ||
                nextAction == MeetDroneMaster_DreamTalk0 ||
                nextAction == MeetDroneMaster_DreamTalk1 ||
                nextAction == MeetDroneMaster_DreamTalk2)
            {
                customSubBehaviourID = MeetDroneMaster;
            }
            else
                customSubBehaviourID = CustomSubBehaviour.CustomSubBehaviourID.General;

            currSubBehavior.NewAction(action, nextAction);
            if (customSubBehaviourID != CustomSubBehaviour.CustomSubBehaviourID.General && customSubBehaviourID != currSubBehavior.ID)
            {
                CustomSubBehaviour subBehavior = null;
                for (int i = 0; i < allSubBehaviors.Count; i++)
                {
                    if (allSubBehaviors[i].ID == customSubBehaviourID)
                    {
                        subBehavior = allSubBehaviors[i];
                        break;
                    }
                }
                if (subBehavior == null)
                {
                    if(customSubBehaviourID == MeetDroneMaster)
                    {
                        subBehavior = new MIFOracleMeetDroneMaster(this);
                    }
                    allSubBehaviors.Add(subBehavior);
                }
                subBehavior.Activate(action, nextAction);
                currSubBehavior.Deactivate();
                Plugin.Log("Switching subbehavior to: " + subBehavior.ID.ToString() + " from: " + this.currSubBehavior.ID.ToString());
                currSubBehavior = subBehavior;
            }
            inActionCounter = 0;
            action = nextAction;
        }

        public override void AddConversationEvents(CustomOracleConversation conv, Conversation.ID id)
        {
            if(id == DroneMaster_DreamTalk0)
            {
                conv.events.Add(new Conversation.TextEvent(conv, 0, "Great success!", 0));
                conv.events.Add(new Conversation.TextEvent(conv, 0, "The way they say it works really works!", 0));
                conv.events.Add(new Conversation.TextEvent(conv, 0, ".  .  .", 0));
                conv.events.Add(new PauseAndWaitForStillEvent(conv, conv.convBehav, 40));
                conv.events.Add(new Conversation.TextEvent(conv, 0, "Oh little one you are awake, how does it feel to be in this world?", 0));
            }
            else if(id == DroneMaster_DreamTalk1)
            {
                conv.events.Add(new Conversation.TextEvent(conv, 0, "The current environment outside is too dangerous even for you.", 0));
                conv.events.Add(new Conversation.TextEvent(conv, 0, "To keep you safe, I made this for you.", 0));
                conv.events.Add(new Conversation.TextEvent(conv, 0, "A drone backpack, although you may not always understand it, but it will protect you to the maximum extent.", 0));
                conv.events.Add(new Conversation.TextEvent(conv, 0, "Also if you are not too far from my precinct, I can talk to you through it.", 0));
                conv.events.Add(new Conversation.TextEvent(conv, 0, ".  .  .", 0));
                conv.events.Add(new Conversation.TextEvent(conv, 0, "I hope you will like it.", 0));
            }
        }
    }

    public class MIFOracleMeetDroneMaster : CustomConversationBehaviour
    {

        #region DreamTalk1
        public LightSource portShowLight;
        public static readonly int movePortCounter = 400;

        public int currentMovePortCounter = 0;
        #endregion
        public MIFOracleMeetDroneMaster(MIFOracleBehaviour owner) : base(owner, MIFOracleBehaviour.MeetDroneMaster, MIFOracleBehaviour.DroneMaster_DreamTalk0)
        {
            owner.getToWorking = 1f;
        }

        public override void Update()
        {
            base.Update();
            if (player == null) return;



            if (action == MIFOracleBehaviour.MeetDroneMaster_Init)
            {
                movementBehavior = CustomMovementBehavior.Idle;
                if (inActionCounter > 300)
                {
                    owner.NewAction(MIFOracleBehaviour.MeetDroneMaster_DreamTalk1);
                    return;
                }
            }
            else if(action == MIFOracleBehaviour.MeetDroneMaster_DreamTalk0)
            {
                if (owner.conversation != null)
                {
                    if (owner.conversation.events.Count < 3)//前三句话说完了
                        movementBehavior = CustomMovementBehavior.Talk;
                    else
                        movementBehavior = CustomMovementBehavior.Idle;

                    if (owner.conversation.slatedForDeletion)
                    {
                        owner.conversation = null;
                        //owner.NewAction(CustomAction.General_Idle);

                        if (RainWorldGamePatch.modules.TryGetValue(owner.oracle.room.game, out var module))
                        {
                            module.EndDroneMasterDream(owner.oracle.room.game);
                        }
                        return;
                    }
                }
            }
            else if(action == MIFOracleBehaviour.MeetDroneMaster_DreamTalk1)
            {
                if (owner.conversation != null)
                {
                    movementBehavior = CustomMovementBehavior.Talk;

                    if (PlayerPatchs.modules.TryGetValue(player, out var pmodule))
                    {

                        if (DronePortOverride.overrides.TryGetValue(pmodule.port, out var overrides) && currentMovePortCounter < movePortCounter)
                        {
                            currentMovePortCounter++;
                            overrides.connectToDMProggress = currentMovePortCounter / (float)movePortCounter;

                            overrides.dronePortGraphicsPos = oracle.firstChunk.pos + Vector2.left * 40f;
                            overrides.dronePortGraphicsRotation += 10f;

                            
                            if (portShowLight == null)
                            {
                                portShowLight = new LightSource(overrides.currentPortPos, false, new Color(255f / 255f, 67f / 255f, 115f / 255f), oracle) { alpha = 10f,rad = 200f};
                                oracle.room.AddObject(portShowLight);
                            }
                            portShowLight.pos = overrides.currentPortPos;
                        }
                        else
                        {
                            if(portShowLight != null)
                            {
                                oracle.room.PlaySound(SoundID.Gate_Clamp_Lock, player.mainBodyChunk, false, 1f, 2.2f + Random.value);
                                oracle.room.AddObject(new ExplosionSpikes(oracle.room,player.mainBodyChunk.pos,5,40f,50,10f,20f,Color.white));

                                portShowLight.Destroy();
                                portShowLight = null;
                            }
                        }
                    }


                    if (owner.conversation.slatedForDeletion)
                    {
                        owner.conversation = null;
                        if (RainWorldGamePatch.modules.TryGetValue(owner.oracle.room.game, out var module))
                        {
                            module.EndDroneMasterDream(owner.oracle.room.game);
                        }
                        return;
                    }
                } 
            }
        }

        public override void NewAction(CustomAction oldAction, CustomAction newAction)
        {
            base.NewAction(oldAction, newAction);
            if (newAction == MIFOracleBehaviour.MeetDroneMaster_DreamTalk0)
            {
                owner.InitateConversation(MIFOracleBehaviour.DroneMaster_DreamTalk0, this);
            }
            else if(newAction == MIFOracleBehaviour.MeetDroneMaster_DreamTalk1)
            {
                owner.InitateConversation(MIFOracleBehaviour.DroneMaster_DreamTalk1, this);
            }
            else if (newAction == CustomAction.General_Idle)
            {
                owner.getToWorking = 1f;
            }
        }
    }
}
