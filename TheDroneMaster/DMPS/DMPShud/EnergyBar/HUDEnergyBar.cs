using DMPS.PlayerHooks;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPShud.EnergyBar
{
    internal class HUDEnergyBar : HUD.HudPart
    {
        DMPSEnergyBarBase energyBar;

        Vector2 pos, lastPos;
        float downInCorner, fade, lastFade;
        int remainShowCount;
        float energy, lowEnergyLim;
        bool expand;

        public HUDEnergyBar(HUD.HUD hud) : base(hud)
        {
            lastPos = pos = new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f));

            energyBar = new DMPSEnergyBarBase(hud.fContainers[1]);
            if (PlayerPatchs.TryGetModule<DMPSModule>((hud.owner as Player), out var module))
            {
                energyBar.TotalEnergy = module.bioReactor.maxReactorEnergy;
            }
        }

        public override void Update()
        {
            base.Update();
            GameUpdate();

            pos = Vector2.Lerp(new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f)), hud.karmaMeter.pos + Custom.DegToVec(Mathf.Lerp(90f, 135f, downInCorner)) * (hud.karmaMeter.Radius + 22f + Custom.SCurve(Mathf.Pow(hud.rainMeter.fade, 0.4f), 0.5f) * 8f), Custom.SCurve(1f - downInCorner, 0.5f));
            if (PlayerPatchs.modules.TryGetValue((hud.owner as Player), out var m) && m is DMPSModule module)
            {
                if (Mathf.Abs(energy - module.bioReactor.reactorEnergy) > 1f)
                    remainShowCount = Mathf.Max(remainShowCount, 80);
                if (fade > .8f)
                    energy = Mathf.Lerp(energy, module.bioReactor.reactorEnergy, 0.25f);
            }
            energyBar.currentEnergy = energy;
            energyBar.pos = pos;
            energyBar.Show = fade;
            energyBar.expand = expand ? 1f : 0f;
            energyBar.alpha = fade;

            energyBar.Update();
        }

        void GameUpdate()
        {
            if (remainShowCount > 0)
                remainShowCount--;


            lastFade = fade;
            if (hud.owner.RevealMap || hud.showKarmaFoodRain || remainShowCount > 0)
            {
                if (hud.owner.RevealMap || hud.showKarmaFoodRain)
                {
                    remainShowCount = 120;
                    expand = true;
                }

                if (fade < 1f)
                    fade = Mathf.Min(1f, fade + 0.05f);
                else
                    fade = Mathf.Max(1f, fade - 0.05f);
            }
            else
            {
                fade = Mathf.Max(0f, fade - 0.05f);
            }

            if (downInCorner > 0f && hud.karmaMeter.AnyVisibility)
            {
                downInCorner = Mathf.Max(0f, downInCorner - 0.0625f);
            }
            else if (fade < 0.2f && hud.karmaMeter.fade == 0f && !hud.karmaMeter.AnyVisibility)
            {
                downInCorner = Mathf.Min(1f, downInCorner + 0.0625f);
            }

            if (fade == 0)
                expand = false;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            energyBar.GrafUpdate(timeStacker);
        }

        public override void ClearSprites()
        {
            energyBar.RemoveSprites();
            base.ClearSprites();
        }
    }
}
