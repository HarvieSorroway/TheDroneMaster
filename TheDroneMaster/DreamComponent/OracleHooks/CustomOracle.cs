using HUD;
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

        public virtual Oracle.OracleID InheritOracleID => null;

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

    

    public class CustomOracleGraphic : OracleGraphics
    {
        public bool callBaseApplyPalette = false;
        public bool callBaseInitiateSprites = false;
        public bool callBaseDrawSprites = false;


        public CustomOracleGraphic(PhysicalObject ow) : base(ow)
        {
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        #region ArmJoinGraphics Modify

        /// <summary>
        /// 用于在ApplyPalette阶段修改 ArmJointGraphics 的 metalColor
        /// </summary>
        /// <param name="armJointGraphics"> ArmJointGraphics 实例 </param>
        /// <param name="sLeaser"></param>
        /// <param name="rCam"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        public virtual Color ArmJoint_MetalColor(ArmJointGraphics armJointGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            return Color.Lerp(palette.blackColor, palette.texture.GetPixel(5, 5), 0.12f);
        }

        /// <summary>
        /// 用于替代 ArmJointGraphics 中的 BaseColor 方法
        /// </summary>
        /// <param name="armJointGraphics">ArmJointGraphics 实例</param>
        /// <param name="pos"> 在房间中的位置，但该位置信息不是每次都会用到 </param>
        /// <returns></returns>
        public virtual Color ArmJoint_BaseColor(ArmJointGraphics armJointGraphics, Vector2 pos)
        {
            return Color.Lerp(Custom.HSL2RGB(0.025f, Mathf.Lerp(0.4f, 0.1f, Mathf.Pow(1f, 0.5f)), Mathf.Lerp(0.05f, 0.7f - 0.5f * owner.room.Darkness(armJointGraphics.myJoint.pos), Mathf.Pow(1f, 0.45f))), new Color(0f, 0f, 0.1f), Mathf.Pow(Mathf.InverseLerp(0.45f, -0.05f, 1f), 0.9f) * 0.5f);
        }

        /// <summary>
        /// 用于替代 ArmJointGraphics 中的 HighLightColor 方法
        /// </summary>
        /// <param name="armJointGraphics">ArmJointGraphics 实例</param>
        /// <param name="pos"> 在房间中的位置，但该位置信息不是每次都会用到 </param>
        /// <returns></returns>
        public virtual Color ArmJoint_HighLightColor(ArmJointGraphics armJointGraphics, Vector2 pos)
        {
            return Color.Lerp(Custom.HSL2RGB(0.025f, Mathf.Lerp(0.5f, 0.1f, Mathf.Pow(1f, 0.5f)), Mathf.Lerp(0.15f, 0.85f - 0.65f * owner.room.Darkness(armJointGraphics.myJoint.pos), Mathf.Pow(1f, 0.45f))), new Color(0f, 0f, 0.15f), Mathf.Pow(Mathf.InverseLerp(0.45f, -0.05f, 1f), 0.9f) * 0.4f);
        }
        #endregion

        #region UbilicalCord Modify
        /// <summary>
        /// UbilicalCord 中的第一种电线颜色
        /// </summary>
        /// <param name="ubilicalCord"> UbilicalCord 实例 </param>
        /// <returns></returns>
        public virtual Color UbilicalCord_WireCol_1(UbilicalCord ubilicalCord)
        {
            return new Color(1f, 0f, 0f);
        }

        /// <summary>
        /// UbilicalCord 中的第二种电线颜色
        /// </summary>
        /// <param name="ubilicalCord"> UbilicalCord 实例 </param>
        /// <returns></returns>
        public virtual Color UbilicalCord_WireCol_2(UbilicalCord ubilicalCord)
        {
            return new Color(0f, 0f, 1f);
        }
        #endregion

        public virtual Color Gown_Color(Gown gown,float f)
        {
            return Custom.HSL2RGB(Mathf.Lerp(0.08f, 0.02f, Mathf.Pow(f, 2f)), Mathf.Lerp(1f, 0.8f, f), 0.5f);
        }
    }


    public class CustomOracleBehaviour : OracleBehavior
    {
        #region 默认字段和属性
        public Vector2 lastPos;
        public Vector2 nextPos;
        public Vector2 lastPosHandle;
        public Vector2 nextPosHandle;
        public Vector2 currentGetTo;

        public float pathProgression;

        public float investigateAngle;

        public float invstAngSpeed;

        public Vector2 baseIdeal;

        public float working;

        public float getToWorking;

        public float killFac;
        public float lastKillFac;

        public bool floatyMovement;

        public int discoverCounter;

        public int throwOutCounter;

        public int playerOutOfRoomCounter;

        public CustomSubBehaviour currSubBehavior;

        public List<CustomSubBehaviour> allSubBehaviors;

        public CustomOracleConversation conversation;

        public CustomMovementBehavior movementBehavior;

        public CustomAction action;

        public float lastKillFacOverseer;

        public float killFacOverseer;

        public bool pearlPickupReaction = true;

        public bool lastPearlPickedUp = true;

        public bool restartConversationAfterCurrentDialoge;

        public bool playerEnteredWithMark;

        public int timeSinceSeenPlayer = -1;
        #endregion

        #region 自定义字段和属性
        public virtual int NotWorkingPalette => 25;
        public virtual int GetWorkingPalette => 26;

        #endregion

        public override DialogBox dialogBox
        {
            get
            {
                if (this.oracle.room.game.cameras[0].hud.dialogBox == null)
                {
                    this.oracle.room.game.cameras[0].hud.InitDialogBox();
                    this.oracle.room.game.cameras[0].hud.dialogBox.defaultYPos = -10f;
                }
                return this.oracle.room.game.cameras[0].hud.dialogBox;
            }
        }

        public CustomOracleBehaviour(Oracle oracle) : base(oracle)
        {
            currentGetTo = oracle.firstChunk.pos;
            lastPos = oracle.firstChunk.pos;
            nextPos = oracle.firstChunk.pos;

            pathProgression = 1f;
            investigateAngle = Random.value * 360f;

            allSubBehaviors = new List<CustomSubBehaviour>();
            currSubBehavior = new NoSubBehavior(this);
            allSubBehaviors.Add(currSubBehavior);

            working = 1f;
            getToWorking = 1f;

            movementBehavior = CustomMovementBehavior.Idle;
            playerEnteredWithMark = oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark;
        }

        public override void Update(bool eu)
        {
            if (timeSinceSeenPlayer >= 0)
            {
                timeSinceSeenPlayer++;
            }
            base.Update(eu);

            if (conversation != null)
            {
                if (restartConversationAfterCurrentDialoge && conversation.paused && dialogBox.messages.Count == 0 && player.room == oracle.room)
                {
                    conversation.paused = false;
                    restartConversationAfterCurrentDialoge = false;
                    conversation.RestartCurrent();
                }
            }
            else
            {
                restartConversationAfterCurrentDialoge = false;
            }

            if (!oracle.Consious) return;
            if (oracle.slatedForDeletetion) return;


            if (conversation != null)
                conversation.Update();

            if (!currSubBehavior.CurrentlyCommunicating)
                pathProgression = UpdatePathProgression();

            currentGetTo = Custom.Bezier(lastPos, ClampVectorInRoom(lastPos + lastPosHandle), nextPos, ClampVectorInRoom(nextPos + nextPosHandle), pathProgression);
            floatyMovement = false;
            investigateAngle += invstAngSpeed;
            inActionCounter++;

            if (player != null && player.room == oracle.room)
            {
                PlayerStayInRoomUpdate();
                playerOutOfRoomCounter = 0;
            }
            else
            {
                PlayerOutOfRoomUpdate();
                killFac = 0f;
                playerOutOfRoomCounter++;
            }

            if (pathProgression >= 1f && consistentBasePosCounter > 100 && !oracle.arm.baseMoving) 
                allStillCounter++;
            else
                allStillCounter = 0;

            lastKillFac = killFac;

            DecideActionUpdate();

            Move();
            if (working != getToWorking)
            {
                working = Custom.LerpAndTick(working, getToWorking, 0.05f, 1f / 30f);
            }

            for (int i = 0; i < oracle.room.game.cameras.Length; i++)
            {
                if (oracle.room.game.cameras[i].room == oracle.room && !oracle.room.game.cameras[i].AboutToSwitchRoom && oracle.room.game.cameras[i].paletteBlend != working)
                {
                    oracle.room.game.cameras[i].ChangeBothPalettes(25, 26, working);
                }
            }

            if (!currSubBehavior.Gravity)
            {
                oracle.room.gravity = Custom.LerpAndTick(oracle.room.gravity, 0f, 0.05f, 0.02f);
            }
            else if (!ModManager.MSC || oracle.room.world.name != "HR" || !oracle.room.game.IsStorySession || !oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.ripMoon || oracle.ID != Oracle.OracleID.SS)
            {
                oracle.room.gravity = 1f - working;
            }
        }

        #region 默认帮助方法
        public Vector2 ClampVectorInRoom(Vector2 v)
        {
            Vector2 vector = v;
            vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
            vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
            return vector;
        }
        #endregion

        #region 自定义帮助方法
        public virtual float UpdatePathProgression()
        {
            return Mathf.Min(1f, pathProgression + 1f / Mathf.Lerp(40f + pathProgression * 80f, Vector2.Distance(lastPos, nextPos) / 5f, 0.5f));
        }
        #endregion

        #region 默认功能方法
        /// <summary>
        /// 见到玩家时的反应，用于加载子行为
        /// </summary>
        public virtual void SeePlayer()
        {
            if (timeSinceSeenPlayer < 0)
                timeSinceSeenPlayer = 0;

            //TODO : coop 支持，但真的不想写所以先去他嘛的
        }

        /// <summary>
        /// 玩家进入房间时调用的方法，手动调用
        /// </summary>
        public virtual void SlugcatEnterRoomReaction()
        {
            getToWorking = 0f;
            oracle.room.PlaySound(SoundID.SS_AI_Exit_Work_Mode, 0f, 1f, 1f);
            if (oracle.graphicsModule != null)
            {
                (oracle.graphicsModule as CustomOracleGraphic).halo.ChangeAllRadi();
                (oracle.graphicsModule as CustomOracleGraphic).halo.connectionsFireChance = 1f;
            }
        }

        public virtual void Move()
        {

        }

        public virtual bool HandTowardsPlayer()
        {
            return false;
        }

        public virtual void ReactToHitWeapon()
        {

        }
        #endregion

        #region 自定义功能方法
        public virtual void PlayerStayInRoomUpdate()
        {

        }

        public virtual void PlayerOutOfRoomUpdate()
        {

        }

        public virtual void DecideActionUpdate()
        {
            if (action == CustomAction.General_Idle)
            {
                if (movementBehavior != CustomMovementBehavior.Idle)
                {
                    movementBehavior = CustomMovementBehavior.Idle;
                }
                throwOutCounter = 0;
                if (player != null && player.room == oracle.room)
                {
                    discoverCounter++;
                    if (oracle.room.GetTilePosition(player.mainBodyChunk.pos).y < 32 && (discoverCounter > 220 || Custom.DistLess(player.mainBodyChunk.pos, oracle.firstChunk.pos, 150f) || !Custom.DistLess(player.mainBodyChunk.pos, oracle.room.MiddleOfTile(oracle.room.ShortcutLeadingToNode(1).StartTile), 150f)))
                    {
                        SeePlayer();
                    }
                }
            }
        }
        #endregion

        public void UnlockShortcuts()
        {
            oracle.room.lockedShortcuts.Clear();
        }

        public class NoSubBehavior : CustomSubBehaviour
        {
            public NoSubBehavior(CustomOracleBehaviour owner) : base(owner, CustomSubBehaviour.CustomSubBehaviourID.General)
            {
            }
        }

        #region 基类
        /// <summary>
        /// 对话类的抽象类，与 SSOracleBehaviour.ConversationBehavior 对应
        /// </summary>
        public abstract class CustomConversationBehaviour : CustomTalkBehaviour
        {
            public CustomConversationBehaviour(CustomOracleBehaviour owner, CustomSubBehaviour.CustomSubBehaviourID ID, Conversation.ID convoID) : base(owner, ID)
            {
                this.convoID = convoID;
            }
            public Conversation.ID convoID;
        }

        /// <summary>
        /// 子行为的基类，与SSOracleBehaviour.SubBehavior对应
        /// </summary>
        public abstract class CustomSubBehaviour
        {
            public CustomSubBehaviourID ID;

            public CustomOracleBehaviour owner;

            public int inActionCounter => owner.inActionCounter;
            public virtual bool CurrentlyCommunicating => false;
            public virtual bool Gravity => true;
            public virtual float LowGravity => -1f;

            public virtual Vector2? LookPoint => null;

            public CustomAction action => owner.action;
            public Oracle oracle => owner.oracle;
            public Player player => owner.player;
            public CustomMovementBehavior movementBehavior
            {
                get
                {
                    return owner.movementBehavior;
                }
                set
                {
                    owner.movementBehavior = value;
                }
            }

            public CustomSubBehaviour(CustomOracleBehaviour owner, CustomSubBehaviourID ID)
            {
                this.owner = owner;
                this.ID = ID;
            }

            public virtual void Update()
            {
            }

            public virtual void NewAction(CustomAction oldAction, CustomAction newAction)
            {
            }

            public virtual void Activate(CustomAction oldAction, CustomAction newAction)
            {
                NewAction(oldAction, newAction);
            }

            public virtual void Deactivate()
            {
                owner.UnlockShortcuts();
            }

            /// <summary>
            /// 子行为ID，结构与其他的ExtEnum类似
            /// </summary>
            public class CustomSubBehaviourID : ExtEnum<CustomSubBehaviourID>
            {
                public CustomSubBehaviourID(string value, bool register = false) : base(value, register)
                {
                }

                public static readonly CustomSubBehaviourID General = new CustomSubBehaviourID("General", true);
            }

        }

        /// <summary>
        /// 具有对话和行为的行为基类，与 SSOracleBehaviour.TalkBehavior 对应
        /// </summary>
        public abstract class CustomTalkBehaviour : CustomSubBehaviour
        {
            public DialogBox dialogBox
            {
                get
                {
                    return this.owner.dialogBox;
                }
            }

            public string Translate(string s)
            {
                return this.owner.Translate(s);
            }

            public override bool CurrentlyCommunicating
            {
                get
                {
                    return this.dialogBox.ShowingAMessage;
                }
            }

            public CustomTalkBehaviour(CustomOracleBehaviour owner, CustomSubBehaviourID ID) : base(owner, ID)
            {
            }

            public override void NewAction(CustomAction oldAction, CustomAction newAction)
            {
                base.NewAction(oldAction, newAction);
                this.communicationIndex = 0;
            }

            public int communicationIndex;

            public int communicationPause;
        }

        /// <summary>
        /// 与 PebblesConversavtion 对应
        /// </summary>
        public class CustomOracleConversation : Conversation
        {
            public CustomOracleBehaviour owner;

            public CustomConversationBehaviour convBehav;

            public bool waitForStill;

            public int age;

            /// <summary>
            /// AddEvents 中调用的事件，用于给指定的ID添加对话
            /// </summary>
            public event AddEventDelegate AddEvent;

            public CustomOracleConversation(CustomOracleBehaviour owner, CustomConversationBehaviour convBehav, ID id, DialogBox dialogBox) : base(owner, id, dialogBox)
            {
                this.owner = owner;
                this.convBehav = convBehav;
                AddEvents();
            }

            public override void Update()
            {
                age++;
                if (this.waitForStill)
                {
                    if (!convBehav.CurrentlyCommunicating && convBehav.communicationPause > 0)
                    {
                        convBehav.communicationPause--;
                    }
                    if (!convBehav.CurrentlyCommunicating && convBehav.communicationPause < 1 && owner.allStillCounter > 20)
                    {
                        waitForStill = false;
                        return;
                    }
                }
                else
                {
                    base.Update();
                }
            }

            public string Translate(string s)
            {
                return owner.Translate(s);
            }

            public override void AddEvents()
            {
                AddEvent.Invoke(id, owner);
            }

            public delegate void AddEventDelegate(ID id, CustomOracleBehaviour owner);

            public class PauseAndWaitForStillEvent : Conversation.DialogueEvent
            {
                public PauseAndWaitForStillEvent(Conversation owner, CustomConversationBehaviour _convBehav, int pauseFrames) : base(owner, 0)
                {
                    this.convBehav = _convBehav;
                    if (this.convBehav == null && owner is CustomOracleConversation)
                    {
                        this.convBehav = (owner as CustomOracleConversation).convBehav;
                    }
                    this.pauseFrames = pauseFrames;
                }

                public override void Activate()
                {
                    base.Activate();
                    this.convBehav.communicationPause = this.pauseFrames;
                    (this.owner as CustomOracleConversation).waitForStill = true;
                }

                public CustomConversationBehaviour convBehav;

                public int pauseFrames;
            }
        }
        #endregion

        #region CustomEnums
        public class CustomAction : ExtEnum<CustomAction>
        {
            public CustomAction(string value, bool register = false) : base(value, register)
            {
            }

            public static readonly CustomAction General_Idle = new CustomAction("General_Idle", true);

        }

        public class CustomMovementBehavior : ExtEnum<CustomMovementBehavior>
        {
            public CustomMovementBehavior (string value, bool register = false) : base(value, register)
            {
            }

            public static readonly CustomMovementBehavior Idle = new CustomMovementBehavior("Idle", true);

            public static readonly CustomMovementBehavior KeepDistance = new CustomMovementBehavior("KeepDistance", true);

            public static readonly CustomMovementBehavior Investigate = new CustomMovementBehavior("Investigate", true);

            public static readonly CustomMovementBehavior Talk = new CustomMovementBehavior("Talk", true);

            public static readonly CustomMovementBehavior ShowMedia = new CustomMovementBehavior("ShowMedia", true);
        }
        #endregion
    }
}
