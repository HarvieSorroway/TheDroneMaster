using DMPS.PlayerHooks;
using JollyCoop;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.PlayerHooks
{
    internal static class DMPSPlayerHooks
    {
        static BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void HooksOn()
        {
            On.Player.UpdateAnimation += Player_UpdateAnimation;
            On.Player.AddFood += Player_AddFood;
            On.Player.AddQuarterFood += Player_AddQuarterFood;

            //On.RainWorldGame.Win += RainWorldGame_Win;
            //throw new NotImplementedException();
            On.Player.FoodInRoom_Room_bool += Player_FoodInRoom_Room_bool;
            On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;
            On.Creature.HypothermiaUpdate += Creature_HypothermiaUpdate;

            new Hook(typeof(Player).GetProperty(nameof(Player.MaxFoodInStomach), PropertyFlags).GetGetMethod(),
                     typeof(DMPSPlayerHooks).GetMethod(nameof(Player_MaxFoodInStomach), BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static int Player_FoodInRoom_Room_bool(On.Player.orig_FoodInRoom_Room_bool orig, Player self, Room checkRoom, bool eatAndDestroy)
        {
            if (PlayerPatchs.TryGetModule<DMPSModule>(self, out _))
            {
                return 2;
            }
            else
                return orig.Invoke(self, checkRoom, eatAndDestroy);
        }

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
        {
            throw new Exception();
            Plugin.Log($"RainWorldGame_Win orig malnourished {malnourished}, play as {self.GetStorySession.saveState.progression.PlayingAsSlugcat}");
            if (self.GetStorySession.saveState.progression.PlayingAsSlugcat == DMEnums.DMPS.SlugStateName.DMPS)
            {
                orig.Invoke(self, false, false);
                Plugin.Log("RainWorldGame_Win no malnourished");
            }
            else
            {
                orig.Invoke(self, malnourished, fromWarpPoint);
                Plugin.Log("RainWorldGame_Win orig malnourished");
            }
        }

        private static void Creature_SuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
        {
            if(self is Player p && PlayerPatchs.TryGetModule<DMPSModule>(p, out DMPSModule m))
            {
                m.port.EnteringShortcut(entrancePos, p);
            }
            orig.Invoke(self, entrancePos, carriedByOther);
        }

        private static void Player_AddQuarterFood(On.Player.orig_AddQuarterFood orig, Player self)
        {
            if (PlayerPatchs.TryGetModule<DMPSModule>(self, out var m))
            {
                m.bioReactor.Charge(0.25f * DMPSBioReactor.food2energyRatio);
            }
            else
                orig.Invoke(self);
        }

        private static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
        {
            if (PlayerPatchs.TryGetModule<DMPSModule>(self, out var m))
            {
                m.bioReactor.Charge(add * DMPSBioReactor.food2energyRatio);
            }
            else
                orig.Invoke(self, add);
        }

        private static void Creature_HypothermiaUpdate(On.Creature.orig_HypothermiaUpdate orig, Creature self)
        {
            orig.Invoke(self);
            if (self is Player p && PlayerPatchs.modules.TryGetValue(p, out var playerModule) && playerModule is DMPSModule m)
            {
                m.HypothermiaUpdate(p);
            }
        }

        private static void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
        {
            orig.Invoke(self);
            if (self.animation == DMEnums.DMPS.PlayerAnimationIndex.DMFlip)
            {
                self.bodyMode = Player.BodyModeIndex.Default;
                Vector2 vector5 = -Custom.PerpendicularVector(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
                self.bodyChunks[0].vel -= vector5 * (float)self.slideDirection * Mathf.Lerp(0.9f, 1.8f, self.Adrenaline) * (self.flipFromSlide ? 2.5f : 1f);
                self.bodyChunks[1].vel += vector5 * (float)self.slideDirection * Mathf.Lerp(0.9f, 1.8f, self.Adrenaline) * (self.flipFromSlide ? 2.5f : 1f);
                self.standing = false;
                for (int n = 0; n < 2; n++)
                {
                    if (self.bodyChunks[n].ContactPoint.x != 0 || self.bodyChunks[n].ContactPoint.y != 0)
                    {
                        self.animation = Player.AnimationIndex.None;
                        self.standing = self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y;
                        return;
                    }
                }
                
                if(self.graphicsModule is PlayerGraphics p)//make tail spin probably
                {
                    foreach(var tail in p.tail)
                    {
                        tail.vel += vector5 * 6f;
                    }
                }
            }
        }
    
        private static int Player_MaxFoodInStomach(Func<Player,int> orig, Player self)
        {
            if(PlayerPatchs.TryGetModule<DMPSModule>(self, out var m) && m.bioReactor.Chargeable)
                return int.MaxValue;
            return orig(self);
        }
    }
}
