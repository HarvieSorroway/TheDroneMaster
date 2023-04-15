using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DreamComponent.OracleHooks
{
    public class CustomOracle
    {
        /// <summary>
        /// 迭代器生成的房间
        /// </summary>
        public virtual string LoadRoom => "";

        /// <summary>
        /// 生成的迭代器ID
        /// </summary>
        public virtual Oracle.OracleID OracleID => Oracle.OracleID.SS;

        /// <summary>
        /// 当前迭代器的gravity
        /// </summary>
        public float gravity = 0.9f;
        
        /// <summary>
        /// 迭代器在房价中的初始位置
        /// </summary>
        public Vector2 startPos = new Vector2(350f, 350f);

        /// <summary>
        /// 最后一次实例化的Oracle弱引用
        /// </summary>
        public WeakReference<Oracle> oracleRef = new WeakReference<Oracle>(null);


        /// <summary>
        /// 加载迭代器的行为和其他模块，比如OracleArm
        /// </summary>
        /// <param name="oracle">迭代器本身</param>
        /// <param name="room">迭代器所在的房间</param>
        public virtual void LoadBehaviourAndSurroundings(ref Oracle oracle,Room room)
        {
        }

        public override string ToString()
        {
            return base.ToString() + " " + OracleID.ToString() + " " + LoadRoom.ToString();
        }

        public virtual OracleGraphics InitCustomOracleGraphic(PhysicalObject ow)
        {
            return new OracleGraphics(ow);
        }
    }

    public class TestSSOrcale : CustomOracle
    {
        public static Oracle.OracleID DMDOracle = new Oracle.OracleID("DMD", true);
        public override string LoadRoom => "DMD_AI";
        public override Oracle.OracleID OracleID => base.OracleID;

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

    public class CustomOracleGraphic : OracleGraphics
    {
        public CustomOracleGraphic(PhysicalObject ow) : base(ow)
        {
        }
    }

    public class TestSSOracleGraphics : CustomOracleGraphic
    {
        public TestSSOracleGraphics(PhysicalObject ow) : base (ow)
        {
            Random.State state = Random.state;
            Random.InitState(56);
            this.totalSprites = 0;
            this.armJointGraphics = new OracleGraphics.ArmJointGraphics[this.oracle.arm.joints.Length];

            for (int i = 0; i < this.oracle.arm.joints.Length; i++)
            {
                this.armJointGraphics[i] = new OracleGraphics.ArmJointGraphics(this, this.oracle.arm.joints[i], this.totalSprites);
                this.totalSprites += this.armJointGraphics[i].totalSprites;
            }


            this.firstUmbilicalSprite = this.totalSprites;
            this.discUmbCord = new OracleGraphics.DisconnectedUbilicalCord(this, this.totalSprites);
            this.totalSprites += this.discUmbCord.totalSprites;
            this.discUmbCord.Reset(this.oracle.firstChunk.pos);


            this.firstBodyChunkSprite = this.totalSprites;
            this.totalSprites += 2;
            this.neckSprite = this.totalSprites;
            this.totalSprites++;
            this.firstFootSprite = this.totalSprites;
            this.totalSprites += 4;


            this.halo = new OracleGraphics.Halo(this, this.totalSprites);
            this.totalSprites += this.halo.totalSprites;
            this.gown = new OracleGraphics.Gown(this);
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
            this.armBase = new OracleGraphics.ArmBase(this, this.firstArmBaseSprite);
            this.totalSprites += this.armBase.totalSprites;
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
            Color color = new Color(0.105882354f, 0.27058825f, 0.34117648f);

            for (int j = 0; j < base.owner.bodyChunks.Length; j++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + j].color = color;
            }
            sLeaser.sprites[this.neckSprite].color = color;
            sLeaser.sprites[this.HeadSprite].color = color;
            sLeaser.sprites[this.ChinSprite].color = color;
            for (int k = 0; k < 2; k++)
            {
                if (this.armJointGraphics.Length == 0)
                {
                    sLeaser.sprites[this.PhoneSprite(k, 0)].color = this.GenericJointBaseColor();
                    sLeaser.sprites[this.PhoneSprite(k, 1)].color = this.GenericJointHighLightColor();
                    sLeaser.sprites[this.PhoneSprite(k, 2)].color = this.GenericJointHighLightColor();
                }
                else
                {
                    sLeaser.sprites[this.PhoneSprite(k, 0)].color = this.armJointGraphics[0].BaseColor(default(Vector2));
                    sLeaser.sprites[this.PhoneSprite(k, 1)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                    sLeaser.sprites[this.PhoneSprite(k, 2)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                }
                sLeaser.sprites[this.HandSprite(k, 0)].color = color;
                if (this.gown != null)
                {
                    for (int l = 0; l < 7; l++)
                    {
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 1] = this.gown.Color(0f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 2] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 3] = this.gown.Color(0f);
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
    }
}
