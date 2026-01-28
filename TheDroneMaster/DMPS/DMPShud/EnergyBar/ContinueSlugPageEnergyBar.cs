using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.MenuHooks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPShud.EnergyBar
{
    internal class ContinueSlugPageEnergyBar : HUD.HudPart
    {
        internal DMPSEnergyBarBase energyBar;

        Menu.SlugcatSelectMenu.SlugcatPageContinue slugcatPageContinue;

        float initEnergy;
        public float TotalWidth => energyBar.TotalWidth;
        public ContinueSlugPageEnergyBar(HUD.HUD hud, Menu.SlugcatSelectMenu.SlugcatPageContinue slugcatPageContinue) : base(hud)
        {
            this.slugcatPageContinue = slugcatPageContinue;
            energyBar = new DMPSEnergyBarBase(hud.fContainers[1]);

            var data = slugcatPageContinue.saveGameData.DMPSData();
            energyBar.TotalEnergy = data.maxEnergy;
            initEnergy = energyBar.currentEnergy = data.energy;
            energyBar.Show = 1f;
            energyBar.alpha = 1f;
            energyBar.expand = 1f;

            energyBar.lastPos = energyBar.pos = slugcatPageContinue.KarmaSymbolPos + new Vector2(slugcatPageContinue.hud.karmaMeter.Radius + 22f + 0.01f, 0.01f);
        }

        public override void Update()
        {
            base.Update();
            energyBar.Update();

            float fade = Mathf.InverseLerp(0.5f, 0f, Mathf.Abs(slugcatPageContinue.NextScroll(1f)));
            energyBar.pos = slugcatPageContinue.KarmaSymbolPos + new Vector2(slugcatPageContinue.hud.karmaMeter.Radius + 22f + 0.01f, 0.01f);
            energyBar.expand = fade;
            energyBar.alpha = fade;
            energyBar.currentEnergy = initEnergy * fade;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            energyBar.GrafUpdate(timeStacker);
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            energyBar.RemoveSprites();
        }
    }
}
