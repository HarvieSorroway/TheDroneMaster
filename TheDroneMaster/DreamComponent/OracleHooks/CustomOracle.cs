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

}
