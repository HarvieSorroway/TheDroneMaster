using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using Random = UnityEngine.Random;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace TheDroneMaster
{
    public static class GatePatchs
    {
        public delegate bool orig_RegionGate_MeetRequirement(RegionGate self);
        static BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
        static BindingFlags methodFlags = BindingFlags.Static | BindingFlags.Public;


        public static void Patch()
        {
            Hook regionGate_get_MeetRequirement_Hook = new Hook(typeof(RegionGate).GetProperty("MeetRequirement", propFlags).GetGetMethod(), typeof(GatePatchs).GetMethod("RegionGate_get_MeetRequirement", methodFlags));
            On.GateKarmaGlyph.Update += GateKarmaGlyph_Update;
        }

        private static void GateKarmaGlyph_Update(On.GateKarmaGlyph.orig_Update orig, GateKarmaGlyph self, bool eu)
        {
            orig.Invoke(self, eu);

            bool GetDroneMaster = false;
            PlayerPatchs.PlayerModule module = null;
            for (int i = 0; i < self.gate.room.game.Players.Count; i++)
            {
                Player player = self.gate.room.game.Players[i].realizedCreature as Player;
                if (player != null && PlayerPatchs.modules.TryGetValue(player, out module) && module.ownDrones)
                {
                    GetDroneMaster = true;
                    break;
                }
            }

            if (ModManager.MSC && self.ShouldPlayCitizensIDAnimation() != 0)
            {
                if (GetDroneMaster)
                {
                    self.animationTicker++;
                    if (self.animationTicker % 3 == 0 && !self.animationFinished)
                    {
                        self.animationIndex++;
                    }
                    if (self.animationTicker % 15 == 0)
                    {
                        self.glyphIndex++;
                        if (self.glyphIndex < 10)
                        {
                            self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Data_Bit, self.pos, 1f, 0.5f + Random.value * 2f);
                        }
                    }
                    if (self.animationIndex > 9)
                    {
                        self.animationIndex = 0;
                    }
                    if (self.glyphIndex >= 10)
                    {
                        self.animationFinished = true;
                    }
                    else
                    {
                        self.animationFinished = false;
                        Vector2 glyphPos = self.pos;
                        glyphPos.x += (float)((self.glyphIndex) % 3 * 9) - 8f;
                        glyphPos.y += (float)((self.glyphIndex) / 3 * 9) - 5f;

                        module.portGraphics.Cast(glyphPos, 2f);
                    }

                    if (self.animationFinished && self.mismatchLabel != null && self.ShouldPlayCitizensIDAnimation() < 0)
                    {
                        self.mismatchLabel.NewPhrase(51);
                        return;
                    }
                }
            }
            else if (ModManager.MSC)
            {
                if (self.mismatchLabel != null)
                {
                    self.mismatchLabel.Destroy();
                    self.mismatchLabel = null;
                }
                self.animationTicker = 0;
                self.glyphIndex = -1;
                self.animationFinished = false;
            }
        }

        public static bool RegionGate_get_MeetRequirement(orig_RegionGate_MeetRequirement orig, RegionGate self)
        {
            bool origResult = orig.Invoke(self);

            if(self.room.world.region.name == "UW" && self.room.abstractRoom.name.Contains("LC") && self.karmaRequirements[(!self.letThroughDir) ? 1 : 0] == MoreSlugcatsEnums.GateRequirement.RoboLock)
            {
                if (self.room.game.Players.Count == 0 || (self.room.game.FirstAlivePlayer.realizedCreature == null && ModManager.CoopAvailable))
                {
                    return origResult;
                }
                Player player;
                if (ModManager.CoopAvailable && self.room.game.AlivePlayers.Count > 0)
                {
                    player = (self.room.game.FirstAlivePlayer.realizedCreature as Player);
                }
                else
                {
                    player = (self.room.game.Players[0].realizedCreature as Player);
                }
                if (player == null) return origResult;

                if(PlayerPatchs.modules.TryGetValue(player,out var module) && module.ownDrones)
                {
                    return true;
                }
            }

            return origResult;
        }
    }
}
