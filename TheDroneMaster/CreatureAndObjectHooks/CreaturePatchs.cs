using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Reflection;
using MonoMod.Utils;
using System.Runtime.CompilerServices;
using MoreSlugcats;

namespace TheDroneMaster
{
    public static class CreaturePatchs
    {
        public static float BaseShockEnergyPoints = 4f;
        public static List<CreatureTracer<WormGrass>> wormGrassTracesr = new List<CreatureTracer<WormGrass>>();
        public static List<CreatureTracer<DaddyCorruption.Bulb>> bulbTracers = new List<CreatureTracer<DaddyCorruption.Bulb>>();

        public static void Patch()
        {
            On.Centipede.Shock += Centipede_Shock;
            On.ScavengerAI.LikeOfPlayer += ScavengerAI_LikeOfPlayer;
            On.DaddyLongLegs.Collide += DaddyLongLegs_Collide;
            On.DaddyCorruption.BulbNibbleAtChunk += DaddyCorruption_BulbNibbleAtChunk;
            On.DaddyTentacle.CollideWithCreature += DaddyTentacle_CollideWithCreature;

            On.DaddyCorruption.Bulb.ctor += Bulb_ctor;
            On.DaddyCorruption.Bulb.Update += Bulb_Update;

            On.WormGrass.ctor += WormGrass_ctor;


            //var dmd = new DynamicMethodDefinition(typeof(DaddyCorruption.Bulb).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            //var il = dmd.GetILProcessor().Body.Instructions.ToList();
            //foreach (var i in il)
            //{
            //    Plugin.Log(i);
            //}
            //try
            //{
            //    IL.DaddyCorruption.Bulb.Update += new ILContext.Manipulator(PatchBulbUpdate);
            //}
            //catch(Exception e)
            //{
            //    Plugin.LogException(e);
            //}
        }

        private static void Bulb_ctor(On.DaddyCorruption.Bulb.orig_ctor orig, DaddyCorruption.Bulb self, DaddyCorruption owner, int spr, bool hasBlackGoo, IntVector2 tile)
        {
            orig.Invoke(self, owner, spr, hasBlackGoo, tile);
            for (int i = bulbTracers.Count - 1; i >= 0; i--)
            {
                if (bulbTracers[i].Creature == self) return;
            }
            bulbTracers.Add(new CreatureTracer<DaddyCorruption.Bulb>(self, bulbTracers));
        }

        private static void WormGrass_ctor(On.WormGrass.orig_ctor orig, WormGrass self, Room room, List<IntVector2> tiles)
        {
            orig.Invoke(self, room, tiles);
            
            for(int i = wormGrassTracesr.Count - 1;i >= 0; i--)
            {
                if (wormGrassTracesr[i].Creature == self) return;
            }
            wormGrassTracesr.Add(new CreatureTracer<WormGrass>(self,wormGrassTracesr));
        }

        public static void PatchBulbUpdate(ILContext il)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            ILCursor ilcursor = new ILCursor(il);
            MoveType moveType = MoveType.After;
            Func<Instruction, bool>[] array = new Func<Instruction, bool>[2];
            array[0] = ((Instruction i) => ILPatternMatchingExt.MatchCallvirt<AbstractCreature>(i, "get_realizedCreature"));
            array[1] = ((Instruction i) => ILPatternMatchingExt.MatchIsinst<DaddyLongLegs>(i));

            bool match = ilcursor.TryGotoNext(moveType, array);
            if (!match) return;
            var label = ilcursor.DefineLabel();

            ilcursor.Emit(OpCodes.Ldarg_0);
            ilcursor.Emit(OpCodes.Ldfld, typeof(DaddyCorruption.Bulb).GetField("owner", flag));
            ilcursor.Emit(OpCodes.Ldfld, typeof(DaddyCorruption).GetField("room", flag));
            ilcursor.Emit(OpCodes.Callvirt, typeof(Room).GetMethod("get_abstractRoom", flag));
            ilcursor.Emit(OpCodes.Ldfld, typeof(AbstractRoom).GetField("creatures", flag));
            ilcursor.Emit(OpCodes.Ldloc_S, (byte)8);
            ilcursor.Emit(OpCodes.Callvirt, typeof(List<AbstractCreature>).GetMethod("get_Item", flag));
            ilcursor.Emit(OpCodes.Callvirt, typeof(AbstractCreature).GetMethod("get_realizedCreature", flag));
            ilcursor.EmitDelegate<Func<Creature, bool>>(c => { return c is Player; });
            ilcursor.Emit(OpCodes.Brtrue, label);
            ilcursor.MarkLabel(label);
        }

        #region DaddyPatch
        private static void Bulb_Update(On.DaddyCorruption.Bulb.orig_Update orig, DaddyCorruption.Bulb self)
        {
            orig.Invoke(self);
            if (self.eatChunk != null && self.eatChunk.owner is LaserDrone)
            {
                self.eatChunk = null;
            }
        }

        private static void DaddyTentacle_CollideWithCreature(On.DaddyTentacle.orig_CollideWithCreature orig, DaddyTentacle self, int tChunk, BodyChunk creatureChunk)
        {
            if (creatureChunk != null && creatureChunk.owner is LaserDrone) return;
            orig.Invoke(self, tChunk, creatureChunk);
        }

        private static void DaddyCorruption_BulbNibbleAtChunk(On.DaddyCorruption.orig_BulbNibbleAtChunk orig, DaddyCorruption self, DaddyCorruption.Bulb bulb, BodyChunk chunk)
        {
            if (chunk != null && chunk.owner is LaserDrone) return;
            orig.Invoke(self, bulb, chunk);
        }

        private static void DaddyLongLegs_Collide(On.DaddyLongLegs.orig_Collide orig, DaddyLongLegs self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (otherObject != null && otherObject is LaserDrone) return;

            Player player = otherObject as Player;
            if (player != null && PlayerPatchs.modules.TryGetValue(player, out var module) && module.ownDrones)
            {
                if(self.Consious && !self.dead && module.playerDeathPreventer.canTakeDownThisDamage(player, "daddy consume"))
                {
                    foreach (var tentacle in self.tentacles)
                    {
                        tentacle.grabChunk = null;
                        tentacle.neededForLocomotion = true;
                        tentacle.SwitchTask(DaddyTentacle.Task.Locomotion);
                    }
                    if(self.eatObjects != null)
                    {
                        self.eatObjects.Clear();
                    }
                    self.stun = 200;
                }
            }

            orig.Invoke(self, otherObject, myChunk, otherChunk);
        }

        #endregion

        private static float ScavengerAI_LikeOfPlayer(On.ScavengerAI.orig_LikeOfPlayer orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            float result = orig.Invoke(self, dRelation);

            if (dRelation.trackerRep != null && dRelation.trackerRep.representedCreature.realizedCreature != null && dRelation.trackerRep.representedCreature.realizedCreature is Player)
            {
                if (PlayerPatchs.modules.TryGetValue(dRelation.trackerRep.representedCreature.realizedCreature as Player, out var module) && module.ownDrones)
                {
                    result = 0f;
                }
            }
            return result;
        }

        private static void Centipede_Shock(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
        {
            Player player = shockObj as Player;
            if(player != null && PlayerPatchs.modules.TryGetValue(player,out var module) && module.ownDrones)
            {
                if (module.fullCharge)
                {
                    orig.Invoke(self, shockObj);
                    module.portGraphics.OverCharge(self.size);
     
                    self.LoseAllGrasps();
                    self.Die();
                }
                else
                {
                    self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);
                    if (self.graphicsModule != null)
                    {
                        (self.graphicsModule as CentipedeGraphics).lightFlash = 1f;
                        for (int i = 0; i < (int)Mathf.Lerp(4f, 8f, self.size); i++)
                        {
                            self.room.AddObject(new Spark(self.HeadChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
                        }
                    }
                    module.portGraphics.Charge(self.size);
                    for (int j = 0; j < self.bodyChunks.Length; j++)
                    {
                        self.bodyChunks[j].vel += Custom.RNV() * 6f * Random.value;
                        self.bodyChunks[j].pos += Custom.RNV() * 6f * Random.value;
                    }

                    float thisEnergyPoints = Mathf.Clamp(self.size * BaseShockEnergyPoints,0.25f,4f);
                    int foodPoint = (int)thisEnergyPoints;
                    int quanterFoodPoint = (int)((thisEnergyPoints - foodPoint) / 0.25f);

                    player.AddFood(foodPoint);
                    for (int i = 0; i < quanterFoodPoint; i++)
                    {
                        player.AddQuarterFood();
                    }

                    self.Stun(200);
                    self.LoseAllGrasps();
                    self.room.AddObject(new CreatureSpasmer(self, false, 200));
                }
                player.Stun(40);
            }
            else orig.Invoke(self, shockObj);
        }


        public class CreatureTracer<T> where T : class
        {
            public WeakReference<T> creature;
            public List<CreatureTracer<T>> allTracers;
            public T Creature
            {
                get
                {
                    bool haveGrass = creature.TryGetTarget(out var result);
                    if (!haveGrass)
                    {
                        allTracers.Remove(this);
                        return null;
                    }
                    return result;
                }
            }

            public CreatureTracer(T creature, List<CreatureTracer<T>> allTracers)
            {
                this.creature = new WeakReference<T>(creature);
                this.allTracers = allTracers;
            }
        }
    }
}
