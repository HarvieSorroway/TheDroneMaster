using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPShud.EnergyBar;

namespace TheDroneMaster.DMPS.DMPShud
{
    //仅作为hook标记使用
    internal class FoodMeterReplacement : HUD.FoodMeter
    {
        internal DMPSEnergyBarBase energyBar;
        public FoodMeterReplacement(HUD.HUD hud, DMPSEnergyBarBase slugPageEnergyBar, int maxFood, int survivalLimit, Player associatedPup = null, int pupNumber = 0) : base(hud, maxFood, survivalLimit, associatedPup, pupNumber)
        {
            energyBar = slugPageEnergyBar;
        }
    }
}
