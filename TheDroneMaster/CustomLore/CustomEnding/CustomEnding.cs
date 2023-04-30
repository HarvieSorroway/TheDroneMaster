using Menu;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.CustomLore.CustomEnding
{
    public class CustomEnding
    {
        public static void PatchOn()
        {
            //On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
            //On.Menu.SlideShow.ctor += SlideShow_ctor;

            //On.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
        }

        private static void SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
            orig.Invoke(self, storyGameCharacter);

            Plugin.LoggerLog(string.Format("{0},{1}", storyGameCharacter, self.manager.menuSetup.startGameCondition));

            if(storyGameCharacter == new SlugcatStats.Name(Plugin.DroneMasterName) && self.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.New)
            {
                self.manager.nextSlideshow = DroneMasterEnums.DroneMasterIntro;
                self.manager.upcomingProcess = ProcessManager.ProcessID.SlideShow;
            }
        }

        private static void SlideShow_ctor(On.Menu.SlideShow.orig_ctor orig, Menu.SlideShow self, ProcessManager manager, Menu.SlideShow.SlideShowID slideShowID)
        {
            orig.Invoke(self, manager, slideShowID);
            Plugin.LoggerLog(string.Format("{0}",slideShowID));
            if(slideShowID == DroneMasterEnums.DroneMasterIntro)
            {
                self.playList = new List<Menu.SlideShow.Scene>();

                //add scene
                if (manager.musicPlayer != null)
                {
                    self.waitForMusic = "NA_19 - Halcyon Memories";
                    self.stall = true;
                    manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, 10f);
                }
                int num = 13;
                int num2 = 2;
                int num3 = 5;
                int num4 = 7;
                self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, 0f, 0f, 0f));
                self.playList.Add(new SlideShow.Scene(MoreSlugcatsEnums.MenuSceneID.Outro_Rivulet1, self.ConvertTime(0, 0, 20), self.ConvertTime(0, 3, 26), self.ConvertTime(0, num, 0)));
                self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, self.ConvertTime(0, num + num2, 0), 0f, 0f));
                SlideShow.Scene scene = new SlideShow.Scene(MoreSlugcatsEnums.MenuSceneID.Outro_Rivulet2L0, self.ConvertTime(0, num + num2, 15), self.ConvertTime(0, num + num2 + 4, 15), self.ConvertTime(0, num + num2 + num3 + num4 + 3, 0));
                scene.AddCrossFade(self.ConvertTime(0, num + num2 + num3, 15), 20);
                scene.AddCrossFade(self.ConvertTime(0, num + num2 + num3 + 1, 85), 20);
                scene.AddCrossFade(self.ConvertTime(0, num + num2 + num3 + 3, 55), 20);
                self.playList.Add(scene);
                self.playList.Add(new SlideShow.Scene(MenuScene.SceneID.Empty, self.ConvertTime(0, num + num2 + num3 + num4 + 4, 0), self.ConvertTime(0, num + num2 + num3 + num4 + 4, 15), self.ConvertTime(0, num + num2 + num3 + num4 + 6, 0)));
                for (int num5 = 1; num5 < self.playList.Count; num5++)
                {
                    self.playList[num5].startAt += 0.6f;
                    self.playList[num5].fadeInDoneAt += 0.6f;
                    self.playList[num5].fadeOutStartAt += 0.6f;
                }
                self.processAfterSlideShow = ProcessManager.ProcessID.Game;
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
