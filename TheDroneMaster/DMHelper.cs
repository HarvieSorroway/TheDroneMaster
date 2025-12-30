using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SharedPhysics;

namespace TheDroneMaster
{
    internal static class DMHelper
    {
        public static float EaseInOutCubic(float f)
        {
            return f < 0.5 ? 4 * f * f * f : 1 - Mathf.Pow(-2 * f + 2, 3) / 2;
        }
        public static float LerpEase(float t)
        {
            return Mathf.Lerp(t, 1f, Mathf.Pow(t, 0.5f));
        }

        public static float ThreatOfCreature(Creature creature, Player player, bool dmps)
        {
            //copy from ThreatDetermination.ThreatOfCreature();
            float danger = creature.Template.dangerousToPlayer;
            if (creature is Cicada && Plugin.instance.config.HateCicadas.Value) danger = 0.5f;
            if (creature.inShortcut) return 0f;
            if (danger == 0f)
            {
                return 0f;
            }
            if (creature.dead)
            {
                return 0f;
            }
            bool visualContact = false;
            float chanceOfFinding = 0f;
            if (creature.abstractCreature.abstractAI != null && creature.abstractCreature.abstractAI.RealAI != null && creature.abstractCreature.abstractAI.RealAI.tracker != null)
            {
                for (int i = 0; i < creature.abstractCreature.abstractAI.RealAI.tracker.CreaturesCount; i++)
                {
                    if (creature.abstractCreature.abstractAI.RealAI.tracker.GetRep(i).representedCreature == player.abstractCreature)
                    {
                        visualContact = creature.abstractCreature.abstractAI.RealAI.tracker.GetRep(i).VisualContact;
                        chanceOfFinding = creature.abstractCreature.abstractAI.RealAI.tracker.GetRep(i).EstimatedChanceOfFinding;
                        break;
                    }
                }
            }
            danger *= Custom.LerpMap(Vector2.Distance(creature.DangerPos, player.mainBodyChunk.pos), 300f, 2400f, 1f, visualContact ? 0.2f : 0f);
            danger *= 1f + Mathf.InverseLerp(300f, 20f, Vector2.Distance(creature.DangerPos, player.mainBodyChunk.pos)) * Mathf.InverseLerp(2f, 7f, creature.firstChunk.vel.magnitude);
            danger *= Mathf.Lerp(0.33333334f, 1f, Mathf.Pow(chanceOfFinding, 0.75f));
            if (creature.abstractCreature.abstractAI != null && creature.abstractCreature.abstractAI.RealAI != null)
            {
                danger *= creature.abstractCreature.abstractAI.RealAI.CurrentPlayerAggression(player.abstractCreature);
            }

            if (creature is Centipede && !dmps)
            {
                danger *= (player.CurrentFood < player.MaxFoodInStomach) ? 0.1f : 1f;
            }
            return danger;
        }

        public static CollisionResult TraceProjectileAgainstBodyChunks(IProjectileTracer projTracer, Room room, Vector2 lastPos, ref Vector2 pos, float rad, int collisionLayer, bool hitAppendages, params PhysicalObject[] exemptObjects)
        {
            float num = float.MaxValue;
            CollisionResult result = new CollisionResult(null, null, null, hitSomething: false, pos);
            int num2 = collisionLayer;
            int num3 = collisionLayer;
            if (collisionLayer < 0)
            {
                num2 = 0;
                num3 = room.physicalObjects.Length - 1;
            }


            for (int i = num2; i <= num3; i++)
            {
                foreach (PhysicalObject item in room.physicalObjects[i])
                {
                    if (exemptObjects.Contains(item) || !item.canBeHitByWeapons || (projTracer != null && !projTracer.HitThisObject(item)))
                    {
                        continue;
                    }

                    bool flag = false;
                    for (int j = 0; j < item.grabbedBy.Count; j++)
                    {
                        if (flag)
                        {
                            break;
                        }

                        flag = exemptObjects.Contains(item.grabbedBy[j].grabber);
                    }

                    if (flag)
                    {
                        continue;
                    }

                    BodyChunk[] bodyChunks = item.bodyChunks;
                    foreach (BodyChunk bodyChunk in bodyChunks)
                    {
                        if (projTracer == null || projTracer.HitThisChunk(bodyChunk))
                        {
                            float num4 = Custom.CirclesCollisionTime(lastPos.x, lastPos.y, bodyChunk.pos.x, bodyChunk.pos.y, pos.x - lastPos.x, pos.y - lastPos.y, rad, bodyChunk.rad);
                            if (num4 > 0f && num4 < 1f && num4 < num)
                            {
                                num = num4;
                                result = new CollisionResult(item, bodyChunk, null, hitSomething: true, Vector2.Lerp(lastPos, pos, num4));
                            }
                        }
                    }

                    if (!hitAppendages || result.chunk != null || item.appendages == null)
                    {
                        continue;
                    }

                    foreach (PhysicalObject.Appendage appendage in item.appendages)
                    {
                        if (!appendage.canBeHit)
                        {
                            continue;
                        }

                        for (int l = 1; l < appendage.segments.Length; l++)
                        {
                            Vector2 vector = Custom.LineIntersection(lastPos, pos, appendage.segments[l - 1], appendage.segments[l]);
                            if (Mathf.InverseLerp(0f, Vector2.Distance(lastPos, pos), Vector2.Distance(lastPos, vector)) < num && Custom.DistLess(vector, lastPos, Vector2.Distance(lastPos, pos)) && Custom.DistLess(vector, pos, Vector2.Distance(lastPos, pos)) && Custom.DistLess(vector, appendage.segments[l - 1], Vector2.Distance(appendage.segments[l - 1], appendage.segments[l])) && Custom.DistLess(vector, appendage.segments[l], Vector2.Distance(appendage.segments[l - 1], appendage.segments[l])))
                            {
                                result = new CollisionResult(item, null, new PhysicalObject.Appendage.Pos(appendage, l - 1, Mathf.InverseLerp(0f, Vector2.Distance(appendage.segments[l - 1], appendage.segments[l]), Vector2.Distance(appendage.segments[l - 1], vector))), hitSomething: true, vector);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
