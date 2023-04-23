using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;

namespace TheDroneMaster.DreamComponent.OracleHooks
{
    public class CustomOracleExtender
    {
        public static List<CustomOracle> customOracles = new List<CustomOracle>();
        public static Dictionary<Oracle.OracleID, CustomOracle> idAndOracles = new Dictionary<Oracle.OracleID, CustomOracle>();
        public static void RegistryCustomOracle(CustomOracle customOracle)
        {
            if (customOracles.Contains(customOracle)) return;
            customOracles.Add(customOracle);

            idAndOracles.Add(customOracle.OracleID, customOracle);
        }
    }


    public class OraclePatch
    {
        public static void PatchOn()
        {
            try
            {
                OraclePatchs();
                OracleGraphicPatchs();
                OracleArmPatch.PatchOn();


                OracleGraphicsModulePatch.PatchOn();
                OracleGraphicsPropertiesPatch.PatchOn();

                On.Room.ReadyForAI += Room_ReadyForAI;
                CustomOracleExtender.RegistryCustomOracle(new TestSSOrcale());
            }
            catch
            {

            }
        }

        public static void OraclePatchs()
        {
            IL.Oracle.ctor += Oracle_ctor;
            On.Oracle.InitiateGraphicsModule += Oracle_InitiateGraphicsModule;
        }


        public static void OracleGraphicPatchs()
        {
            IL.OracleGraphics.ctor += OracleGraphics_ctor;
            IL.OracleGraphics.InitiateSprites += OracleGraphics_InitiateSprites;
            IL.OracleGraphics.ApplyPalette += OracleGraphics_ApplyPalette;
   


        }

        //private static void ChangeSStoCustom(ILContext il)
        //{
        //    ILCursor cAfter = new ILCursor(il);
        //    ILCursor cBefore = new ILCursor(il);
        //    ILCursor cSkip = new ILCursor(il);


        //    var list1 = new Func<Instruction, bool>[3]{  i => i.MatchCall<OracleGraphics>("get_oracle"),
        //                                                    i => i.MatchLdfld<Oracle>("oracleBehavior"),
        //                                                    i => i.MatchIsinst<SSOracleBehavior>() ,
        //                                                    };
        //    List<byte> vars = new List<byte>();
        //    byte customCount = (byte)il.Method.Body.Variables.Count;
        //    for (byte i = 0; i < il.Method.Body.Variables.Count; i++)
        //    {
        //        if (il.Method.Body.Variables[i].VariableType.Name == typeof(SSOracleBehavior).Name)
        //        {
        //            Debug.Log("[IL HOOK] SS " + i);
        //            vars.Add(i);
        //        }
        //    }
        //    il.Method.Body.Variables.Add(new VariableDefinition(il.IL.Body.Method.Module.ImportReference(typeof(CustomOracleBehaviour))));

        //    for (int j = 0; j < vars.Count + 1; j++)
        //    {
        //        Debug.Log("[IL HOOK] Page " + j);
        //        var curList = j == 0 ? list1 : new Func<Instruction, bool>[] { i => i.MatchLdloc(vars[j - 1]) };


       
        //        while (cAfter.TryGotoNext(MoveType.After, curList))
        //        {
        //            if (!cSkip.TryGotoNext(MoveType.After, i => i == cAfter.Next))
        //                throw new Exception("skip Cursor Error");

        //            if (!cBefore.TryGotoNext(MoveType.Before, curList))
        //                throw new Exception("Before Cursor Error");

        //            var customLabel = cBefore.DefineLabel();
        //            var ssLabel = cBefore.DefineLabel();

        //            if (cAfter.Next.OpCode != OpCodes.Call && cAfter.Next.OpCode != OpCodes.Callvirt && cAfter.Next.OpCode != OpCodes.Ldfld && cAfter.Next.OpCode != OpCodes.Stloc_S)
        //            {
        //                if (!cBefore.TryGotoNext(MoveType.After, i => i == cAfter.Next))
        //                    throw new Exception("Before Cursor Error 2");
        //                continue;
        //            }


        //            cBefore.EmitDelegate<Func<OracleGraphics, bool>>((graphic) => graphic.oracle.oracleBehavior is CustomOracleBehaviour);
        //            cBefore.Emit(OpCodes.Brtrue_S, customLabel);//跳过标准ss获取
        //            cBefore.Emit(OpCodes.Ldarg_0);

        //            cSkip.Emit(OpCodes.Br_S, ssLabel);//跳过自定义获取
        //            cSkip.MarkLabel(customLabel);
        //            if (j == 0)
        //            {
        //                cSkip.Emit(OpCodes.Ldarg_0);
        //                cSkip.EmitDelegate<Func<OracleGraphics, CustomOracleBehaviour>>(Graphics => Graphics.oracle.oracleBehavior as CustomOracleBehaviour);
        //            }
        //            else
        //                cSkip.Emit(OpCodes.Ldloc_S, customCount);
        //            if (cAfter.Next.MatchCall(out var method))
        //            {
        //                Debug.Log("[IL HOOK] Apply Call " + method.Name +
        //                    " " + (typeof(CustomOracleBehaviour).GetMethod(method.Name) != null).ToString() +
        //                    " " + (typeof(CustomOracleBehaviour).GetProperty(method.Name) != null).ToString());

        //                if (typeof(CustomOracleBehaviour).GetMethod(method.Name) != null)
        //                    cSkip.Emit(OpCodes.Call, typeof(CustomOracleBehaviour).GetMethod(method.Name));
        //                else if (typeof(CustomOracleBehaviour).GetProperty(method.Name) != null)
        //                    cSkip.Emit(OpCodes.Call, typeof(CustomOracleBehaviour).GetProperty(method.Name));
        //                else
        //                    cSkip.EmitDelegate<Func<CustomOracleBehaviour, object>>((a) => null);
        //            }
        //            else if (cAfter.Next.MatchCallvirt(out var virtMethod))
        //            {
        //                Debug.Log("[IL HOOK] Apply Callvirt " + virtMethod.Name +
        //                    " " + (typeof(CustomOracleBehaviour).GetMethod(virtMethod.Name) != null).ToString() +
        //                    " " + (typeof(CustomOracleBehaviour).GetProperty(virtMethod.Name) != null).ToString());

        //                if (typeof(CustomOracleBehaviour).GetMethod(virtMethod.Name) != null)
        //                    cSkip.Emit(OpCodes.Callvirt, typeof(CustomOracleBehaviour).GetMethod(virtMethod.Name));
        //                else if (typeof(CustomOracleBehaviour).GetProperty(virtMethod.Name) != null)
        //                    cSkip.Emit(OpCodes.Callvirt, typeof(CustomOracleBehaviour).GetProperty(virtMethod.Name));
        //                else
        //                    cSkip.EmitDelegate<Func<CustomOracleBehaviour, object>>((a) => null);
        //            }
        //            else if (cAfter.Next.MatchLdfld(out var field))
        //            {
        //                Debug.Log("[IL HOOK] Apply Ldfld " + field.Name +
        //                    " " + (typeof(CustomOracleBehaviour).GetField(field.Name) != null).ToString());

        //                if (typeof(CustomOracleBehaviour).GetField(field.Name) != null &&
        //                    typeof(CustomOracleBehaviour).GetField(field.Name).FieldType.Name == field.FieldType.Name)
        //                    cSkip.Emit(OpCodes.Ldfld, typeof(CustomOracleBehaviour).GetField(field.Name));
        //                else if (field.FieldType.IsValueType)
        //                {
        //                    cSkip.EmitDelegate<Action<CustomOracleBehaviour>>((a) => { });
        //                    if (field.FieldType.Name == "string")
        //                        cSkip.Emit(OpCodes.Ldstr, "");
        //                    else
        //                        cSkip.Emit(OpCodes.Ldc_I4, 0);

        //                }
        //                else
        //                {
        //                    Debug.Log("[IL HOOK] Add null to Stack");
        //                    cSkip.EmitDelegate<Func<CustomOracleBehaviour, object>>((a) => null);
        //                }
        //            }
        //            else if (cAfter.Next.MatchStloc(out var a))
        //            {
        //                Debug.Log("[IL HOOK] Apply Stloc");
        //                cSkip.Emit(OpCodes.Stloc_S, customCount);
        //            }
        //            if (!cBefore.TryGotoNext(MoveType.After, i => i == cAfter.Next))
        //                throw new Exception("Before Cursor Error 2");

        //            cSkip.MarkLabel(ssLabel);
        //        }

        //    }
        //    Debug.Log("--------------[IL HOOK] End Apply-------------");
        //    foreach (var a in il.Method.Body.Instructions)
        //    {
        //        try
        //        {
        //            Debug.Log(a.ToString());
        //        }
        //        catch
        //        {
        //            Debug.Log(a.OpCode.Name + " ________");
        //        }
        //    }

        //}

        private static void OracleGraphics_InitiateSprites(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);//用于找到 IL_05e7,跳过默认的sprite初始化
            ILCursor c2 = new ILCursor(il);//用于插入委托

            ILLabel skipLabel = null;
            try
            {
                if (c1.TryGotoNext(MoveType.After,
                    i => i.MatchCall<OracleGraphics>("get_IsPebbles"),
                    i => i.Match(OpCodes.Brtrue_S),
                    i => i.MatchLdarg(0),
                    i => i.MatchCall<OracleGraphics>("get_IsPastMoon")
                ))
                {
                    skipLabel = c1.MarkLabel();
                }

            }
            catch (Exception ex1)
            {
                Debug.LogException(ex1);
                return;
            }
            try
            {
                c2.Emit(OpCodes.Ldarg, 0);
                c2.EmitDelegate<Func<OracleGraphics, bool>>((graphic) =>
                {
                    if (graphic is CustomOracleGraphic)
                    {
                        Plugin.Log("Skip current OracleGraphic InitiateSprites ? " + (!(graphic as CustomOracleGraphic).callBaseInitiateSprites).ToString());
                        return !(graphic as CustomOracleGraphic).callBaseInitiateSprites;
                    }
                    return false;
                });
                c2.Emit(OpCodes.Brtrue_S, skipLabel);
            }
            catch (Exception ex2)
            {
                Debug.LogException(ex2);
                return;
            }
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
                    c2.EmitDelegate<Func<OracleGraphics, bool>>((graphic) =>
                    {
                        if (graphic is CustomOracleGraphic)
                        {
                            Plugin.Log("Skip current OracleGraphic ApplyPalette ? " + (!(graphic as CustomOracleGraphic).callBaseApplyPalette).ToString());
                            return !(graphic as CustomOracleGraphic).callBaseApplyPalette;
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
                if (c2.TryGotoNext(MoveType.After,
                    i => i.MatchCall<GraphicsModule>(".ctor"),
                    i => i.Match(OpCodes.Call),
                    i => i.MatchStloc(0)
                ))
                {
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.Emit(OpCodes.Ldarg_1);
                    c2.EmitDelegate<Func<OracleGraphics, PhysicalObject, bool>>((graphic, ow) =>
                    {
                        if (graphic is CustomOracleGraphic)
                        {
                            Plugin.Log("Skip current OracleGraphic ctor");
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
            catch (Exception ex)
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
                        foreach (var customOracle in CustomOracleExtender.customOracles)
                        {
                            if (room.abstractRoom.name == customOracle.LoadRoom)
                            {
                                Plugin.Log(String.Format("Load Custom Oracle\nroom : {0}\nID : {1}", customOracle.LoadRoom, customOracle.OracleID));
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
            catch (Exception ex2)
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
                if (c5.TryGotoNext(MoveType.After,
                    i => i.MatchCall<PhysicalObject>("set_buoyancy")
                ))
                {
                    c5.Emit(OpCodes.Ldarg_0);
                    c5.Emit(OpCodes.Ldarg, 2);
                    c5.EmitDelegate<Func<Oracle, Room, bool>>((self, room) =>
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
            if (self.game != null && self.game.IsStorySession)
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

        public static Func<Oracle.OracleID, Oracle.OracleID, bool> JudgeOracleID = (orig, origCondition) =>
        {
            if (CustomOracleExtender.idAndOracles.TryGetValue(orig, out var customOracle) && customOracle.InheritOracleID != null)
            {
                //Plugin.Log(String.Format("CustomOracle : {0}, origCondition : {1}, inheritOracleID : {2}", orig, origCondition, customOracle.InheritOracleID));
                return (orig == origCondition) || (customOracle.InheritOracleID == origCondition);
            }
            return orig == origCondition;
        };

        public static void AddCustomIDJudgement(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);

            while (c1.TryGotoNext(MoveType.After,
                     i => i.MatchLdarg(0),
                     i => i.MatchLdfld<Oracle.OracleArm>("oracle"),
                     i => i.MatchLdfld<Oracle>("ID"),
                     i => i.Match(OpCodes.Ldsfld)
            ))
            {
                c1.Remove();
                c1.EmitDelegate(JudgeOracleID);
            }
        }
    }


    public class OracleArmPatch
    {
        public static void PatchOn()
        {
            IL.Oracle.OracleArm.Update += OracleArm_Update;
            IL.Oracle.OracleArm.BaseDir += OracleArm_BaseDir;
            IL.Oracle.OracleArm.OnFramePos += OracleArm_OnFramePos;

            IL.Oracle.OracleArm.Update += OracleArm_Update1;
        }

        private static void OracleArm_Update1(ILContext il)
        {
            try
            {
                OraclePatch.AddCustomIDJudgement(il);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void OracleArm_OnFramePos(ILContext il)
        {
            try
            {
                OraclePatch.AddCustomIDJudgement(il);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void OracleArm_BaseDir(ILContext il)
        {
            try
            {
                OraclePatch.AddCustomIDJudgement(il);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void OracleArm_Update(ILContext il)
        {
            try
            {
                OraclePatch.AddCustomIDJudgement(il);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

    }

    public class OracleGraphicsPropertiesPatch
    {
        static BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
        static BindingFlags methodFlags = BindingFlags.Static | BindingFlags.Public;

        public static void PatchOn()
        {
            Hook isPebblesHook = new Hook(typeof(OracleGraphics).GetProperty("IsPebbles", propFlags).GetGetMethod(), typeof(OracleGraphicsPropertiesPatch).GetMethod("get_IsPebbles"));
            Hook isIsMoonHook = new Hook(typeof(OracleGraphics).GetProperty("IsPebbles", propFlags).GetGetMethod(), typeof(OracleGraphicsPropertiesPatch).GetMethod("get_IsMoon"));
        }

        public static bool get_IsPebbles(Func<OracleGraphics, bool> orig, OracleGraphics self)
        {
            bool result = orig.Invoke(self);
            if (CustomOracleExtender.idAndOracles.TryGetValue(self.oracle.ID, out var customOracle) && customOracle.InheritOracleID != null)
            {
                result |= customOracle.InheritOracleID == Oracle.OracleID.SS;
            }
            return result;
        }

        public static bool get_IsMoon(Func<OracleGraphics, bool> orig, OracleGraphics self)
        {
            bool result = orig.Invoke(self);
            if (CustomOracleExtender.idAndOracles.TryGetValue(self.oracle.ID, out var customOracle) && customOracle.InheritOracleID != null)
            {
                result |= customOracle.InheritOracleID == Oracle.OracleID.SL;
            }
            return result;
        }
    }
}
