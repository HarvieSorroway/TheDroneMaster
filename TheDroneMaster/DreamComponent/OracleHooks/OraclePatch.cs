using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace TheDroneMaster.DreamComponent.OracleHooks
{
    public class OraclePatch
    {
        public static void PatchOn()
        {
            OraclePatchs();
            OracleGraphicPatchs();
            On.Room.ReadyForAI += Room_ReadyForAI;
            CustomOracleExtender.RegistryCustomOracle(new TestSSOrcale());
        }

        public static void OraclePatchs()
        {
            IL.Oracle.ctor += Oracle_ctor;

            On.Oracle.InitiateGraphicsModule += Oracle_InitiateGraphicsModule;
        }


        public static void OracleGraphicPatchs()
        {
            IL.OracleGraphics.ctor += OracleGraphics_ctor;
            IL.OracleGraphics.ApplyPalette += OracleGraphics_ApplyPalette;
        }

        private static void OracleGraphics_ApplyPalette(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);//用于找到 ret
            ILCursor c2 = new ILCursor(il);//用于找到基类构造函数，并在其后插入委托

            ILLabel retLabel = null;
            try
            {
                c1.Index = il.Instrs.Count - 1;
                retLabel = c1.MarkLabel();
            }
            catch (Exception ex1)
            {
                Debug.LogException(ex1);
                return;
            }

            try
            {
                if (c2.TryGotoNext(MoveType.After,
                    i => i.MatchCall<GraphicsModule>("ApplyPalette")
                ))
                {
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.Emit(OpCodes.Ldarg_1);
                    c2.EmitDelegate<Func<OracleGraphics, PhysicalObject, bool>>((graphic, ow) =>
                    {
                        if (graphic is CustomOracleGraphic)
                        {
                            Plugin.Log("Skip current OracleGraphic ApplyPalette");
                            return true;
                        }
                        return false;
                    });
                    c2.Emit(OpCodes.Brtrue_S, retLabel);
                }
            }
            catch (Exception ex2)
            {
                Debug.LogException(ex2);
                return;
            }
        }

        private static void OracleGraphics_ctor(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);//用于找到 ret
            ILCursor c2 = new ILCursor(il);//用于找到基类构造函数，并在其后插入委托

            ILLabel retLabel = null;
            try
            {
                c1.Index = il.Instrs.Count - 1;
                retLabel = c1.MarkLabel();
            }
            catch (Exception ex1)
            {
                Debug.LogException(ex1);
                return;
            }

            try
            {
               if(c2.TryGotoNext(MoveType.After,
                   i => i.MatchCall<GraphicsModule>(".ctor"),
                   i => i.Match(OpCodes.Call),
                   i => i.MatchStloc(0)
               ))
               {
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.Emit(OpCodes.Ldarg_1);
                    c2.EmitDelegate<Func<OracleGraphics, PhysicalObject, bool>>((graphic, ow) =>
                    {
                        if(graphic is CustomOracleGraphic)
                        {
                            Plugin.Log("Skip current OracleGraphic ctor");
                            return true;
                        }
                        return false;
                    });
                    c2.Emit(OpCodes.Brtrue_S, retLabel);
               }
            }
            catch(Exception ex2)
            {
                Debug.LogException(ex2);
                return;
            }
        }

        #region Oracle
        private static void Oracle_ctor(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);//用于找到 IL_0301, 跳过默认的ID加载
            ILCursor c2 = new ILCursor(il);//用于找到Bodychunks初始化，并插入委托

            ILCursor c4 = new ILCursor(il);//用于找到 IL_0736, 返回函数
            ILCursor c5 = new ILCursor(il);//用于加载OracleBehaviour和周边物品

            ILLabel skipLabel = null;
            ILLabel retLabel = null;
            try
            {
                if (c1.TryGotoNext(MoveType.After,
                    x => x.MatchLdloc(0),
                    x => x.MatchLdarg(0),
                    x => x.Match(OpCodes.Call),
                    x => x.MatchLdlen(),
                    x => x.MatchConvI4(),
                    x => x.Match(OpCodes.Blt_S)
                 ))
                {
                    skipLabel = c1.MarkLabel();
                    if (skipLabel == null)
                    {
                        Plugin.Log("Can't find skip label!");
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                return;
            }

            try
            {
                if (skipLabel != null && c2.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdcI4(2),
                    i => i.MatchNewarr<BodyChunk>(),
                    i => i.Match(OpCodes.Call)
                ))
                {
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.Emit(OpCodes.Ldarg, 2);
                    c2.EmitDelegate<Func<Oracle, Room, bool>>((self, room) =>
                    {
                        foreach(var customOracle in CustomOracleExtender.customOracles)
                        {
                            if (room.abstractRoom.name == customOracle.LoadRoom)
                            {
                                Plugin.Log(String.Format("Load Custom Oracle\nroom : {0}\nID : {1}", customOracle.LoadRoom,customOracle.OracleID));
                                self.ID = customOracle.OracleID;
                                customOracle.oracleRef.SetTarget(self);

                                for (int i = 0; i < self.bodyChunks.Length; i++)
                                {
                                    self.bodyChunks[i] = new BodyChunk(self, i, customOracle.startPos, 6f, 0.5f);
                                }

                                return true;
                            }
                        }
                        return false;
                    });
                    c2.Emit(OpCodes.Brtrue_S, skipLabel);
                }
            }
            catch(Exception ex2)
            {
                Debug.LogException(ex2);
                return;
            }

            try
            {
                c4.Index = il.Instrs.Count - 1;
                retLabel = c4.MarkLabel();
            }
            catch (Exception ex4)
            {
                Debug.LogException(ex4);
            }

            try
            {
                if(c5.TryGotoNext(MoveType.After,
                    i => i.MatchCall<PhysicalObject>("set_buoyancy")
                ))
                {
                    c5.Emit(OpCodes.Ldarg_0);
                    c5.Emit(OpCodes.Ldarg, 2);
                    c5.EmitDelegate<Func<Oracle, Room,bool>>((self, room) =>
                    {
                        foreach (var customOracle in CustomOracleExtender.customOracles)
                        {
                            if (room.abstractRoom.name == customOracle.LoadRoom)
                            {
                                self.gravity = customOracle.gravity;
                                Plugin.Log("Load custom oracle baviour and surroundings");
                                customOracle.LoadBehaviourAndSurroundings(ref self, room);
                               
                                return true;
                            }
                        }
                        return false;
                    });
                }
                c5.Emit(OpCodes.Brtrue_S, retLabel);
            }
            catch (Exception ex5)
            {
                Debug.LogException(ex5);
            }
        }

        private static void Oracle_InitiateGraphicsModule(On.Oracle.orig_InitiateGraphicsModule orig, Oracle self)
        {
            foreach (var customOracle in CustomOracleExtender.customOracles)
            {
                if (customOracle.oracleRef.TryGetTarget(out var oracle) && oracle == self)
                {
                    if (self.graphicsModule == null) self.graphicsModule = customOracle.InitCustomOracleGraphic(self);
                    return;
                }
            }
            orig.Invoke(self);
        }
        #endregion

        private static void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig.Invoke(self);
            if(self.game != null && self.game.IsStorySession)
            {
                foreach (var customOracle in CustomOracleExtender.customOracles)
                {
                    if (customOracle.LoadRoom == self.abstractRoom.name)
                    {
                        Oracle oracle = new Oracle(new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(self.abstractRoom.index, 15, 15, -1), self.game.GetNewID()), self);
                        self.AddObject(oracle);
                        Plugin.Log(String.Format("Create Oracle for ID : {0}", customOracle.OracleID));
                    }
                }
            }
        }
    }

    public class CustomOracleExtender
    {
        public static List<CustomOracle> customOracles = new List<CustomOracle>();
        public static void RegistryCustomOracle(CustomOracle customOracle)
        {
            if (customOracles.Contains(customOracle)) return;
            customOracles.Add(customOracle);
        }
    }
}
