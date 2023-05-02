using Menu;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.CustomLore.CustomEnding
{
    public class CustomEnding
    {
        public static void PatchOn()
        {
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
            On.Menu.SlideShow.ctor += SlideShow_ctor;

            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            On.Menu.MenuDepthIllustration.ctor += MenuDepthIllustration_ctor;

            On.Menu.SlugcatSelectMenu.SlugcatPage.AddAltEndingImage += SlugcatPage_AddAltEndingImage;
        }

        private static void SlugcatPage_AddAltEndingImage(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddAltEndingImage orig, SlugcatSelectMenu.SlugcatPage self)
        {
            if(self.slugcatNumber == new SlugcatStats.Name(Plugin.DroneMasterName))
            {
                var sceneID = DroneMasterEnums.TheDroneMaster_AltEndScene;
                self.slugcatDepth = 3f;
                self.sceneOffset = new Vector2(10f, 75f);

                self.sceneOffset.x = self.sceneOffset.x - (1366f - self.menu.manager.rainWorld.options.ScreenSize.x) / 2f;
                self.slugcatImage = new InteractiveMenuScene(self.menu, self, sceneID);
                self.subObjects.Add(self.slugcatImage);
            }
        }

        private static void MenuDepthIllustration_ctor(On.Menu.MenuDepthIllustration.orig_ctor orig, MenuDepthIllustration self, Menu.Menu menu, MenuObject owner, string folderName, string fileName, Vector2 pos, float depth, MenuDepthIllustration.MenuShader shader)
        {
            orig.Invoke(self, menu, owner, folderName, fileName, pos, depth, shader);
            MenuScene scene = owner as MenuScene;
            if(scene != null && scene.sceneID == DroneMasterEnums.TheDroneMaster_Outro1 || scene.sceneID == DroneMasterEnums.TheDroneMaster_Outro2 || scene.sceneID == DroneMasterEnums.TheDroneMaster_Outro3 || scene.sceneID == DroneMasterEnums.TheDroneMaster_AltEndScene)
            {
                float scale = Screen.width / 1920f;
                self.sprite.scaleX *= scale;
                self.sprite.scaleY *= scale;
            }
        }

        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig.Invoke(self);
            if(self.sceneID == DroneMasterEnums.TheDroneMaster_Outro1)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar + "slugcat - thedronemaster" + Path.DirectorySeparatorChar + "outro1";

                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-1-background", Vector2.left * 500f * Vector2.down * 400f, 100f,   MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-1-screen"    , Vector2.left * 500f * Vector2.down * 200f, 3f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-1-pearl"     , Vector2.left * 500f * Vector2.down * 200f, 2f,   MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-1-ai"        , Vector2.left * 500f * Vector2.down * 200f, 3f, MenuDepthIllustration.MenuShader.Normal));
            }
            else if(self.sceneID == DroneMasterEnums.TheDroneMaster_Outro2)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar + "slugcat - thedronemaster" + Path.DirectorySeparatorChar + "outro2";

                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-2-0-background", Vector2.zero, 10f, MenuDepthIllustration.MenuShader.Normal));
                //for (int i = 1; i < 5; i++)
                //{
                //    self.AddCrossfade(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-2-" + i.ToString() + "-background", Vector2.zero, 3f, MenuDepthIllustration.MenuShader.Normal));
                //}

                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-2-0-screen", Vector2.zero, 3f, MenuDepthIllustration.MenuShader.Normal));
                for (int i = 1; i < 5; i++)
                {
                    self.AddCrossfade(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-2-" + i.ToString() + "-screen", Vector2.zero, 3f, MenuDepthIllustration.MenuShader.Normal));
                }

                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-2-0-pearls", Vector2.zero, 5f, MenuDepthIllustration.MenuShader.Normal));

                for (int i = 1; i < 5; i++)
                {
                    self.AddCrossfade(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-2-" + i.ToString() + "-pearls", Vector2.zero, 5f, MenuDepthIllustration.MenuShader.Normal));
                }
            }
            else if(self.sceneID == DroneMasterEnums.TheDroneMaster_Outro3)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar + "slugcat - thedronemaster" + Path.DirectorySeparatorChar + "outro3";

                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-3-background", Vector2.left * 500f * Vector2.down * 400f, 100f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-3-pearl", Vector2.left * 500f * Vector2.down * 200f, 3f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-3-ai", Vector2.left * 500f * Vector2.down * 200f, 2f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dronemaster-outro-3-screen", Vector2.left * 500f * Vector2.down * 200f, 100f, MenuDepthIllustration.MenuShader.Normal));
            }
            else if(self.sceneID == DroneMasterEnums.TheDroneMaster_AltEndScene)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar + "slugcat - thedronemaster" + Path.DirectorySeparatorChar + "altend";

                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dmAltEnd-sky", Vector2.zero, 2.8f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dmAltEnd-wifi", Vector2.zero, 2.5f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dmAltEnd-slugcat", Vector2.zero, 2f, MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "dmAltEnd-shadow", Vector2.zero, 2f, MenuDepthIllustration.MenuShader.Normal));
            }
        }

        private static void SlideShow_ctor(On.Menu.SlideShow.orig_ctor orig, Menu.SlideShow self, ProcessManager manager, Menu.SlideShow.SlideShowID slideShowID)
        {
            orig.Invoke(self, manager, slideShowID);
            Plugin.LoggerLog(string.Format("{0}",slideShowID));
            if(slideShowID == DroneMasterEnums.DroneMasterAltEnd)
            {
                self.playList = new List<Menu.SlideShow.Scene>();

                //add scene
                if (manager.musicPlayer != null)
                {
                    self.waitForMusic = "RW_Outro_Theme_B";
                    self.stall = true;
                    manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, 10f);
                }
                int num = 13;
                int num2 = 2;
                int num3 = 5;
                int num4 = 7;

                self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, 0f, 0f, 0f));
                self.playList.Add(new SlideShow.Scene(DroneMasterEnums.TheDroneMaster_Outro1, self.ConvertTime(0, 1, 20), self.ConvertTime(0, 4, 26), self.ConvertTime(0, 15, 0)));
                //self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, self.ConvertTime(0, 7, 0), 0f, 0f));

                SlideShow.Scene scene = new SlideShow.Scene(DroneMasterEnums.TheDroneMaster_Outro2, self.ConvertTime(0, 16, 15), self.ConvertTime(0, 19, 15), self.ConvertTime(0, 30, 0));
                scene.AddCrossFade(self.ConvertTime(0, 20, 15), 15);
                scene.AddCrossFade(self.ConvertTime(0, 21, 65), 15);
                scene.AddCrossFade(self.ConvertTime(0, 23, 15), 15);
                scene.AddCrossFade(self.ConvertTime(0, 24, 65), 15);
                self.playList.Add(scene);

                //self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, self.ConvertTime(0, 30, 0), 0f, 0f));
                self.playList.Add(new SlideShow.Scene(DroneMasterEnums.TheDroneMaster_Outro3, self.ConvertTime(0, 31, 20), self.ConvertTime(0, 34, 26), self.ConvertTime(0, 42, 0)));
                self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, self.ConvertTime(0, 45, 0), 0f, 0f));

                for (int num5 = 1; num5 < self.playList.Count; num5++)
                {
                    self.playList[num5].startAt += 0.6f;
                    self.playList[num5].fadeInDoneAt += 0.6f;
                    self.playList[num5].fadeOutStartAt += 0.6f;
                }
                self.processAfterSlideShow = ProcessManager.ProcessID.Credits;
                //...more and more

                self.preloadedScenes = new SlideShowMenuScene[self.playList.Count];
                for (int i = 0; i < self.preloadedScenes.Length; i++)
                {
                    self.preloadedScenes[i] = new SlideShowMenuScene(self, self.pages[0], self.playList[i].sceneID);
                    self.preloadedScenes[i].Hide();
                }

                self.current = 0;
                self.NextScene();

                Plugin.LoggerLog(string.Format("{0},{1},{2}", slideShowID, self.current, self.scene));
            }
            
        }

        private static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            if(self.GetStorySession.saveState.saveStateNumber == new SlugcatStats.Name(Plugin.DroneMasterName))
            {
                if (self.manager.upcomingProcess != null)
                    return;
                if (self.manager.musicPlayer != null)
                    self.manager.musicPlayer.FadeOutAllSongs(20f);

                self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);

                self.manager.statsAfterCredits = true;
                self.manager.nextSlideshow = DroneMasterEnums.DroneMasterAltEnd;
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
                return;
            }
            orig.Invoke(self);
        }
    }
}
