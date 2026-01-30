using CustomSaveTx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPShud;
using TheDroneMaster.DMPS.DMPShud.EnergyBar;
using TheDroneMaster.DMPS.DMPSSave;

namespace TheDroneMaster.DMPS.MenuHooks
{
    internal static class ContinueSlugPageHooks
    {
        static ConditionalWeakTable<Menu.SlugcatSelectMenu.SaveGameData, DMPSSaveGameData> dmpsDataTable = new ConditionalWeakTable<Menu.SlugcatSelectMenu.SaveGameData, DMPSSaveGameData>();
        public static void HooksOn()
        {
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
            On.Menu.SlugcatSelectMenu.MineForSaveData += SlugcatSelectMenu_MineForSaveData; 
            
            On.HUD.FoodMeter.TotalWidth += FoodMeter_TotalWidth;
        }

        private static float FoodMeter_TotalWidth(On.HUD.FoodMeter.orig_TotalWidth orig, HUD.FoodMeter self, float timeStacker)
        {
            if (self is FoodMeterReplacement foodMeterReplacement)
            {
                return foodMeterReplacement.energyBar.TotalWidth;
            }
            if (self.circles.Count == 0)
                return 0f;
            else
                return orig.Invoke(self, timeStacker);
        }

        private static void SlugcatPageContinue_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig.Invoke(self, menu, owner, pageIndex, slugcatNumber);
            if (slugcatNumber == DMEnums.DMPS.SlugStateName.DMPS)
            {
                var energyBarHudPart = new ContinueSlugPageEnergyBar(self.hud, self);
                self.hud.AddPart(energyBarHudPart);

                self.hud.foodMeter.ClearSprites();
                self.hud.foodMeter = null;

                self.hud.AddPart(new FoodMeterReplacement(self.hud, energyBarHudPart.energyBar, SlugcatStats.SlugcatFoodMeter(slugcatNumber).x, SlugcatStats.SlugcatFoodMeter(slugcatNumber).y, null, 0));
            }
        }

        private static Menu.SlugcatSelectMenu.SaveGameData SlugcatSelectMenu_MineForSaveData(On.Menu.SlugcatSelectMenu.orig_MineForSaveData orig, ProcessManager manager, SlugcatStats.Name slugcat)
        {
            var data = orig.Invoke(manager, slugcat);
            
            if (!manager.rainWorld.progression.IsThereASavedGame(slugcat) || slugcat != DMEnums.DMPS.SlugStateName.DMPS)
            {
                return data;
            }
            DMPSBasicSave dmpsSave;
            if (manager.rainWorld.progression.currentSaveState != null && manager.rainWorld.progression.currentSaveState.saveStateNumber == slugcat)
            {
                dmpsSave = DeathPersistentSaveDataRx.GetTreatmentOfType<DMPSBasicSave>();
                var dmpsData = new DMPSSaveGameData()
                {
                    energy = dmpsSave.Energy,
                    maxEnergy = dmpsSave.MaxEnergy,
                };
                dmpsDataTable.Add(data, dmpsData);
            }
            if (!manager.rainWorld.progression.HasSaveData)
            {
                return data;
            }
            string[] progLinesFromMemory = manager.rainWorld.progression.GetProgLinesFromMemory();
            if (progLinesFromMemory.Length == 0)
            {
                return data;
            }

            dmpsSave = new DMPSBasicSave(DMEnums.DMPS.SlugStateName.DMPS);
            string header = DeathPersistentSaveDataRx.TotalHeader + dmpsSave.header;
            foreach (var progLine in progLinesFromMemory)
            {
                if (!progLine.Contains(header))
                    continue;
                string[] array = Regex.Split(progLine, "<dpA>");

                foreach(var line in array)
                {
                    if (line.Contains(header))
                    {
                        string[] array2 = Regex.Split(line, "<dpB>");
                        dmpsSave.LoadDatas(array2[1]);
                        var dmpsData = new DMPSSaveGameData()
                        {
                            energy = dmpsSave.Energy,
                            maxEnergy = dmpsSave.MaxEnergy,
                        };
                        dmpsDataTable.Add(data, dmpsData);
                        break;
                    }
                }
            }

            return data;
        }

        public static bool TryGetDMPSData(Menu.SlugcatSelectMenu.SaveGameData saveGameData, out DMPSSaveGameData dmpsData)
        {
            if(dmpsDataTable.TryGetValue(saveGameData, out dmpsData))
            {
                return true;
            }
            return false;
        }

        public static DMPSSaveGameData DMPSData(this Menu.SlugcatSelectMenu.SaveGameData saveGameData)
        {
            if(TryGetDMPSData(saveGameData, out var dmpsData))
            {
                return dmpsData;
            }
            return null;
        }

        internal class DMPSSaveGameData
        {
            public float energy = -1;
            public int maxEnergy = -1;
        }
    }
}
