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
            On.RoofTopView.ctor += RoofTopView_ctor;

        }
        private static void RoofTopView_ctor(On.RoofTopView.orig_ctor orig, RoofTopView self, Room room, RoomSettings.RoomEffect effect)
        {
            if (room.abstractRoom.name.Contains("DMD"))
            {
                self.floorLevel = 26f;
                self.room = room;
                self.elements = new List<BackgroundScene.BackgroundSceneElement>();
                self.atmosphereColor = new Color(0.16078432f, 0.23137255f, 0.31764707f);
                //self.atmosphereColor = LaserDroneGraphics.defaultLaserColor * 0.5f + Color.black * 0.5f;

                self.effect = effect;
                self.sceneOrigo = self.RoomToWorldPos(room.abstractRoom.size.ToVector2() * 10f) + Vector2.down * 250f;
                room.AddObject(new RoofTopView.DustpuffSpawner());
                self.daySky = new BackgroundScene.Simple2DBackgroundIllustration(self, "Rf_Sky", new Vector2(683f, 384f));

                self.duskSky = new BackgroundScene.Simple2DBackgroundIllustration(self, "Rf_DuskSky", new Vector2(683f, 384f));

                self.nightSky = new BackgroundScene.Simple2DBackgroundIllustration(self, "Rf_NightSky", new Vector2(683f, 384f));
                self.isLC = (ModManager.MSC && ((room.world.region != null && room.world.region.name == "LC") || self.room.abstractRoom.name.StartsWith("LC_")));
 
                string text = "_DMD";
                bool flag = false;

                if (self.room.dustStorm)
                {
                    self.dustWaves = new List<RoofTopView.DustWave>();
                    float num = 2500f;
                    float num2 = 0f;
                    self.dustWaves.Add(new RoofTopView.DustWave(self, "RF_CityA" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(300f + (flag ? -300f : 0f), 0f), -num2).x, self.floorLevel / 4f - num * 40f), 370f, 0f));
                    self.dustWaves.Add(new RoofTopView.DustWave(self, "RF_CityA" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(300f + (flag ? 300f : 0f), 0f), -num2).x, self.floorLevel / 5f - num * 30f), 290f, 0f));
                    self.dustWaves.Add(new RoofTopView.DustWave(self, "RF_CityA" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(300f + (flag ? -300f : 0f), 0f), -num2).x, self.floorLevel / 6f - num * 20f), 210f, 0f));
                    self.dustWaves.Add(new RoofTopView.DustWave(self, "RF_CityA" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(300f + (flag ? -300f : 0f), 0f), -num2).x, self.floorLevel / 7f - num * 10f), 130f, 0f));
                    RoofTopView.DustWave dustWave = new RoofTopView.DustWave(self, "RF_CityA" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(300f + (flag ? -300f : 0f), 0f), -num2).x, self.floorLevel / 8f), 50f, 0f);
                    dustWave.isTopmost = true;
                    self.dustWaves.Add(dustWave);
                    foreach (RoofTopView.DustWave element in self.dustWaves)
                    {
                        self.AddElement(element);
                    }
                }
                if (self.isLC)
                {
                    self.daySky = new BackgroundScene.Simple2DBackgroundIllustration(self, "AtC_Sky", new Vector2(683f, 384f));
                    self.duskSky = new BackgroundScene.Simple2DBackgroundIllustration(self, "AtC_DuskSky", new Vector2(683f, 384f));
                    self.nightSky = new BackgroundScene.Simple2DBackgroundIllustration(self, "AtC_NightSky", new Vector2(683f, 384f));
                    self.AddElement(self.nightSky);
                    self.AddElement(self.duskSky);
                    self.AddElement(self.daySky);
                    self.floorLevel = self.room.world.RoomToWorldPos(new Vector2(0f, 0f), self.room.abstractRoom.index).y - 30992.8f;
                    self.floorLevel *= 22f;
                    self.floorLevel = -self.floorLevel;
                    float num3 = self.room.world.RoomToWorldPos(new Vector2(0f, 0f), self.room.abstractRoom.index).x - 11877f;
                    num3 *= 0.01f;
                    Shader.SetGlobalVector("_AboveCloudsAtmosphereColor", self.atmosphereColor);
                    Shader.SetGlobalVector("_MultiplyColor", Color.white);
                    Shader.SetGlobalVector("_SceneOrigoPosition", self.sceneOrigo);
                    self.AddElement(new RoofTopView.Building(self, "city2", new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(880f, 0f), 200f - num3).x, self.floorLevel * 0.2f - 170000f), 420.5f, 2f));
                    self.AddElement(new RoofTopView.Building(self, "city1", new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(880f, 0f), 70f - num3 * 0.5f).x, self.floorLevel * 0.25f - 116000f), 340f, 2f));
                    self.AddElement(new RoofTopView.Building(self, "city3", new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(880f, 0f), 70f - num3 * 0.5f).x, self.floorLevel * 0.3f - 85000f), 260f, 2f));
                    self.AddElement(new RoofTopView.Building(self, "city2", new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(880f, 0f), 40f - num3 * 0.5f).x, self.floorLevel * 0.35f - 42000f), 180f, 2f));
                    self.AddElement(new RoofTopView.Building(self, "city1", new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(880f, 0f), 90f - num3 * 0.2f).x, self.floorLevel * 0.4f + 5000f), 100f, 2f));
                    self.AddElement(new RoofTopView.Floor(self, "floor", new Vector2(0f, self.floorLevel * 0.2f - 90000f), 400.5f, 500.5f));
                    return;
                }
                self.AddElement(self.nightSky);
                self.AddElement(self.duskSky);
                self.AddElement(self.daySky);
                Shader.SetGlobalVector("_MultiplyColor", Color.white);
                self.AddElement(new RoofTopView.Floor(self, "floor", new Vector2(0f, self.floorLevel), 1f, 12f));
                Shader.SetGlobalVector("_AboveCloudsAtmosphereColor", self.atmosphereColor);
                Shader.SetGlobalVector("_SceneOrigoPosition", self.sceneOrigo);
                for (int i = 0; i < 16; i++)
                {
                    float f = (float)i / 15f;
                    self.AddElement(new RoofTopView.Rubble(self, "Rf_Rubble", new Vector2(0f, self.floorLevel), Mathf.Lerp(1.5f, 8f, Mathf.Pow(f, 1.5f)), i));
                }
                self.AddElement(new RoofTopView.DistantBuilding(self, "Rf_HoleFix", new Vector2(-2676f, 9f), 1f, 0f));

                self.AddElement(new RoofTopView.Building(self, "city2", new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(1780f, 0f), 11.5f).x, self.floorLevel), 11.5f, 3f));
                self.AddElement(new RoofTopView.Building(self, "city1", new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(880f, 0f), 10.5f).x, self.floorLevel), 10.5f, 3f));


                self.AddElement(new RoofTopView.DistantBuilding(self, "RF_CityA" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(300f + (flag ? -300f : 0f), 0f), 8.5f).x, self.floorLevel - 25.5f), 8.5f, 0f));
                self.AddElement(new RoofTopView.DistantBuilding(self, "RF_CityB" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(515f + (flag ? -300f : 0f), 0f), 6.5f).x, self.floorLevel - 13f), 6.5f, 0f));
                self.AddElement(new RoofTopView.DistantBuilding(self, "RF_CityC" + text, new Vector2(self.PosFromDrawPosAtNeutralCamPos(new Vector2(400f + (flag ? -300f : 0f), 0f), 5f).x, self.floorLevel - 8.5f), 5f, 0f));
                self.LoadGraphic("smoke1", false, false);
                self.AddElement(new RoofTopView.Smoke(self, new Vector2(0f, self.floorLevel + 560f), 7f, 0, 2.5f, 0.1f, 0.8f, false));
                self.AddElement(new RoofTopView.Smoke(self, new Vector2(0f, self.floorLevel), 4.2f, 0, 0.2f, 0.1f, 0f, true));
                self.AddElement(new RoofTopView.Smoke(self, new Vector2(0f, self.floorLevel + 28f), 2f, 0, 0.5f, 0.1f, 0f, true));
                self.AddElement(new RoofTopView.Smoke(self, new Vector2(0f, self.floorLevel + 14f), 1.2f, 0, 0.75f, 0.1f, 0f, true));

                Plugin.Log("Modify RoofTop view");
            }
            else
            {
                orig.Invoke(self, room, effect);
            }
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
