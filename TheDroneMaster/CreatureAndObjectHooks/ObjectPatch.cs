using IL.RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.CreatureAndObjectHooks
{
    public class ObjectPatch
    {
        public static void PatchOn()
        {
            SuperStructureFix.PatchOn();
        }
    }

    public class SuperStructureFix
    {
        public static void PatchOn()
        {
            On.SuperStructureFuses.InitiateSprites += SuperStructureFuses_InitiateSprites;
        }

        private static void SuperStructureFuses_InitiateSprites(On.SuperStructureFuses.orig_InitiateSprites orig, SuperStructureFuses self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self,sLeaser, rCam);
            if (self.room.abstractRoom.name.Contains("DMD"))
            {
                int num = 0;
                for (int i = 0; i < self.lights.GetLength(0); i++)
                {
                    for (int j = 0; j < self.lights.GetLength(1); j++)
                    {
                        int x = (int)(i / 2f) + self.rect.left;
                        int y = (int)(j / 2f) + self.rect.bottom;

                        var tile = self.room.GetTile(x, y);

                        bool anySlopeOrAir = false;
                        for (int xx = -1; xx <= 1; xx++)
                        {
                            for (int yy = -1; yy <= 1; yy++)
                            {
                                anySlopeOrAir |= (self.room.GetTile(x + xx, y + yy).Terrain == Room.Tile.TerrainType.Slope || !self.room.GetTile(x + xx, y + yy).Solid);
                            }
                        }


                        float modifyDepth = (tile.Solid && !anySlopeOrAir && tile.Terrain != Room.Tile.TerrainType.Slope) ? (1f - (2f / 90f)) : 0f;
                        Plugin.Log("Solid : {0},depth : {1},tile:{2}-{3}, AnySlopeOrAir : {4}", tile.Solid, modifyDepth, (int)(i / 2f) + self.rect.left, (int)(j / 2f) + self.rect.bottom, anySlopeOrAir);
                        sLeaser.sprites[num].alpha = modifyDepth;
                        num++;
                    }
                }
                self.broken = 0f; 
                Plugin.Log("Apply for {0} sprites", num);
            } 
        }
    }
}
