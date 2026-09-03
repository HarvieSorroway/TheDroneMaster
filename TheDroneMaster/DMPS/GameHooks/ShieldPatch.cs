using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using static Creature;
using static PhysicalObject;

namespace TheDroneMaster.DMPS.GameHooks

{
    public static partial class DMPSGamePatch
    {
        public static void ViolenceWithShield(this Creature creature, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, Appendage.Pos onAppendagePos, DamageType type, float damage, float stunBonus, float shieldDamage)
        {
            shieldModules.GetValue(creature.abstractCreature, c => new CreatureShieldModule(c)).Violence(creature, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus, shieldDamage);
        }

    }
    public static partial class DMPSGamePatch
    {
        private static ConditionalWeakTable<AbstractCreature, CreatureShieldModule> shieldModules = new();

        private static readonly MethodInfo ViolenceHookEntry = typeof(DMPSGamePatch).GetMethod(nameof(ViolenceHook), BindingFlags.NonPublic | BindingFlags.Static)!;

        private static Dictionary<(CreatureTemplate.Type, int), float> shieldSettings = new();

        private static readonly List<Hook> violenceHooks = new();

        internal static void ShieldPatchOn()
        {
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in ass.GetTypes())
                    {
                        PatchCreatureIfHasViolence(type);
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var type in e.Types.Where(i => i != null))
                    {
                        PatchCreatureIfHasViolence(type);
                    }
                }
            }
            if (File.Exists(AssetManager.ResolveFilePath("creatures/shieldsettings.txt")))
            {
                var lines = File.ReadAllLines(AssetManager.ResolveFilePath("creatures/shieldsettings.txt"));
                foreach (var line in lines)
                {
                    var split = Regex.Split(line, @",");
                    if (split.Length < 2)
                        continue;
                    if (float.TryParse(split[1], out var shieldValue))
                    {
                        var critType = new CreatureTemplate.Type(split[0]);
                        shieldSettings[(critType, 0)] = shieldValue;
                        Plugin.Log($"ShieldPatch: Set shield value for {critType} to {shieldValue}");
                    }
                }
            }

        }


        private static void PatchCreatureIfHasViolence(Type type)
        {
            if (!typeof(Creature).IsAssignableFrom(type))
                return;
            if (type.GetMethod("Violence", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public) is { } method)
            {
                violenceHooks.Add(new Hook(method, ViolenceHookEntry.MakeGenericMethod(type)));
            }
        }


        private static void ViolenceHook<T>(Action<T, BodyChunk, Vector2?, BodyChunk, Appendage.Pos, DamageType, float, float> orig, 
            T self, BodyChunk source, Vector2? directionAndMomentum, 
            BodyChunk hitChunk, Appendage.Pos hitAppendage, DamageType type, float damage, float stunBonus) 
            where T : Creature
        {
            if ((source.owner is Creature && source.owner is not Player) ||
                (source.owner is Weapon weapon && weapon.thrownBy is not Player) ||
                (self.abstractCreature.world.game.StoryCharacter.value != "dmps"))
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                return;
            }

            var module = shieldModules.GetValue(self.abstractCreature, c => new CreatureShieldModule(self.abstractCreature));

            if (module.BypassShieldCalc == 0)
            {
                Plugin.Log($"CalcShieldViolence:{self.Template.type}, Method:{orig.Method.DeclaringType.FullName}::{orig.Method.Name}");
                module.CalcShieldViolence(ref source, ref directionAndMomentum, ref hitChunk, ref hitAppendage, ref type, ref damage, ref stunBonus);
            }
            try
            {
                module.BypassShieldCalc++;
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            }
            finally
            {
                module.BypassShieldCalc--;
                module.ShieldDamage = -1;
            }
        }

       


        public class CreatureShieldModule
        {
            private readonly WeakReference<AbstractCreature> creatureRef;
            private float shieldValue = 0;


            public int BypassShieldCalc { get; internal set; }
            public float ShieldDamage { get; internal set; } = -1f;


            internal CreatureShieldModule(AbstractCreature creature)
            {
                creatureRef = new WeakReference<AbstractCreature>(creature);
                shieldValue = shieldSettings.TryGetValue((creature.creatureTemplate.type, 0), out var value2) ? value2 : DefaultShieldValue(creature);
            }

            internal void Violence(Creature crit, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, Appendage.Pos onAppendagePos, DamageType type, float damage, float stunBonus, float shieldDamage)
            {
                ShieldDamage = shieldDamage;
                if (creatureRef.TryGetTarget(out var creature))
                {
                    crit.Violence(source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
                }
            }

            internal void CalcShieldViolence(ref BodyChunk source, ref Vector2? directionAndMomentum,
                ref BodyChunk hitChunk, ref Appendage.Pos onAppendagePos, ref DamageType type, ref float damage, ref float stunBonus)
            {
                ShieldDamage = ShieldDamage == -1 ? DefaultShieldDamage(type, damage) : ShieldDamage;
                // TODO:添加神秘自定义处理
            }

            private float DefaultShieldValue(AbstractCreature creature)
            {
                //TODO: 添加
                return 0.0f;
            }

            private float DefaultShieldDamage(DamageType type, float damage)
            {
                //TODO: 添加
                return 0.0f;
            }
        }

    }
}