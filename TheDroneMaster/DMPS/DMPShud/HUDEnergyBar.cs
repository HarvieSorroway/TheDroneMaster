using CustomSaveTx;
using DMPS.PlayerHooks;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPShud.EnergyBar;
using TheDroneMaster.DMPS.DMPSSave;
using TheDroneMaster.DMPS.PlayerHooks.BioReactors;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPShud
{
    internal class HUDEnergyBar : HUD.HudPart
    {
        DMPSEnergyBarBase energyBar;
        DMPSBasicSave.BioReactorType barMode;

        Vector2 pos, lastPos;
        float downInCorner, fade, lastFade;
        int remainShowCount;
        float energy, lowEnergyLim;
        bool expand;

        public HUDEnergyBar(HUD.HUD hud) : base(hud)
        {
            lastPos = pos = new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f));

            
            if (PlayerPatchs.TryGetModule<DMPSModule>((hud.owner as Player), out var module))
            {
                var save = DeathPersistentSaveDataRx.GetTreatmentOfType<DMPSBasicSave>();

                barMode = save.ReactorType;
                energyBar = save.ReactorType switch
                {
                    DMPSBasicSave.BioReactorType.Default => new DMPSEnergyBarBase(hud.fContainers[1]),
                    DMPSBasicSave.BioReactorType.OverDrive => new OverDriveBar(hud.fContainers[1]),
                    DMPSBasicSave.BioReactorType.Feedback => new FeedBackBar(hud.fContainers[1]),
                    _ => new DMPSEnergyBarBase(hud.fContainers[1])
                };

            }
        }

        public override void Update()
        {
            base.Update();
            GameUpdate();

            //pos = Vector2.Lerp(
            //    new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f)), 
            //    hud.karmaMeter.pos + Custom.DegToVec(Mathf.Lerp(90f, 135f, downInCorner)) * (hud.karmaMeter.Radius + 22f + Custom.SCurve(Mathf.Pow(hud.rainMeter.fade, 0.4f), 0.5f) * 8f), 
            //    Custom.SCurve(1f - downInCorner, 0.5f));

            pos = hud.karmaMeter.pos + Vector2.right * (hud.karmaMeter.Radius + 22f + Custom.SCurve(Mathf.Pow(hud.rainMeter.fade, 0.4f), 0.5f) * 8f)  /*+ Custom.DegToVec(Mathf.Lerp(90f, 135f, downInCorner)) * */;

            if (PlayerPatchs.modules.TryGetValue((hud.owner as Player), out var m) && m is DMPSModule module)
            {
                if (Mathf.Abs(energy - module.energyBarMessage.currentEnergy) > 1f)
                    remainShowCount = Mathf.Max(remainShowCount, 80);
                energy = Mathf.Lerp(energy, module.energyBarMessage.currentEnergy, 0.25f);
                energyBar.TotalEnergy = module.energyBarMessage.totalEnergy;

                switch (barMode)
                {
                    case DMPSBasicSave.BioReactorType.OverDrive:
                        (energyBar as OverDriveBar).setOverDriveEnergy = (module.energyBarMessage as OverDriveMessage).overDriveEnergy;
                        break;
                    case DMPSBasicSave.BioReactorType.Feedback:
                        (energyBar as FeedBackBar).setFeedBackEfficiency = (module.energyBarMessage as FeedBackMessage).feedBackEfficiency;
                        (energyBar as FeedBackBar).Pluse((module.energyBarMessage as FeedBackMessage).pluseStack);
                        break;
                }
            }
            energyBar.currentEnergy = energy;
            energyBar.pos = pos;
            energyBar.Show = 1f;
            energyBar.expand = 1f;
            energyBar.alpha = 1f;

            energyBar.Update();
        }

        void GameUpdate()
        {
            if (remainShowCount > 0)
                remainShowCount--;

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
