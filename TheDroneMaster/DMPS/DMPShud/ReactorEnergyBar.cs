using DMPS.PlayerHooks;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.PlayerHooks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheDroneMaster.DMPS.DMPShud
{
    internal class ReactorEnergyBar : HUD.HudPart
    {
        //params
        static Vector2 pipSize = new Vector2(2f, 20f);
        static float gap = 1f;

        Mode mode;

        Vector2 pos, lastPos;

        FSprite[] energyPips;
        Color maxEnergyColor, lowEnergyColor;

        float energy, lowEnergyLim;
        int flooredEnergy;

        float downInCorner, fade, lastFade, targetRevealHeight, revealHeight;
        int remainShowCount;

        public ReactorEnergyBar(HUD.HUD hud) : base(hud)
        {
            if (hud.owner is Player)
            {
                mode = Mode.InGame;
            }
            else
                mode = Mode.Menu;
            lastPos = pos = new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f));

            if(mode == Mode.InGame)
            {
                lowEnergyColor = Color.cyan;

                if (PlayerPatchs.TryGetModule<DMPSModule>((hud.owner as Player), out var module))
                {
                    maxEnergyColor = module.laserColor;
                    energyPips = new FSprite[module.bioReactor.maxReactorEnergy];
                    lowEnergyLim = module.bioReactor.lowEnergyLim;
                }
                
            }
            else if (mode == Mode.Menu)
            {
                maxEnergyColor = LaserDroneGraphics.defaultLaserColor;
                energyPips = new FSprite[100];
            }

            for (int i = 0;i < energyPips.Length;i++)
            {
                energyPips[i] = new FSprite("pixel", true)
                {
                    scaleX = pipSize.x,
                    scaleY = pipSize.y,
                    color = maxEnergyColor
                };
                hud.fContainers[1].AddChild(energyPips[i]);
            }
            remainShowCount = 80;
        }

        public override void Update()
        {
            base.Update();
            GameUpdate();

            lastPos = pos;
            pos = Vector2.Lerp(new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f)), hud.karmaMeter.pos + Custom.DegToVec(Mathf.Lerp(90f, 135f, downInCorner)) * (hud.karmaMeter.Radius + 22f + Custom.SCurve(Mathf.Pow(hud.rainMeter.fade, 0.4f), 0.5f) * 8f), Custom.SCurve(1f - downInCorner, 0.5f));
            if (PlayerPatchs.modules.TryGetValue((hud.owner as Player), out var m) && m is DMPSModule module)
            {
                if(Mathf.Abs(energy - module.bioReactor.reactorEnergy) > 1f)
                    remainShowCount = Mathf.Max(remainShowCount, 80);
                if(fade > .8f)
                    energy = Mathf.Lerp(energy, module.bioReactor.reactorEnergy, 0.25f);
                flooredEnergy = Mathf.FloorToInt(energy);
            }
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
                    targetRevealHeight = 1f;
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
                targetRevealHeight = 0.3f;
            revealHeight = Mathf.Lerp(revealHeight, targetRevealHeight, 0.25f);
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            Vector2 drawPos = DrawPos(timeStacker);
            float smoothFade = Mathf.Lerp(lastFade, fade, timeStacker);
            Color color = Color.Lerp(lowEnergyColor, maxEnergyColor, Mathf.InverseLerp(0f, lowEnergyLim, energy));
            float flash = (0.8f + 0.2f * Random.value) * smoothFade;
            float currShowAt = Mathf.Pow(smoothFade, 2f) * energyPips.Length;

            for (int i = 0;i < energyPips.Length; i++)
            {
                energyPips[i].SetPosition(drawPos.x + pipSize.x * i + gap * (i + 1), drawPos.y);

                if (i > flooredEnergy)
                {
                    energyPips[i].color = color * 0.6f;
                    energyPips[i].alpha = flash * 0.5f;
                    energyPips[i].scaleY = pipSize.y * Mathf.InverseLerp(0f, i, currShowAt) * revealHeight;
                }
                else if (i < flooredEnergy)
                {
                    energyPips[i].color = color;
                    energyPips[i].alpha = flash;
                    energyPips[i].scaleY = pipSize.y * Mathf.InverseLerp(0f, i, currShowAt) * revealHeight;
                }
                else if(i == flooredEnergy)
                {
                    energyPips[i].color = color;
                    float decimalEnergy = energy - flooredEnergy;
                    float f = flash * Mathf.Lerp(decimalEnergy, 1f, Random.value);
                    energyPips[i].alpha = f;
                    energyPips[i].scaleY = pipSize.y * decimalEnergy * Mathf.Clamp01(currShowAt - i) * revealHeight;
                }
            }
        }

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(this.lastPos, this.pos, timeStacker);
        }

        enum Mode
        {
            InGame,
            Menu
        }
    }
}
