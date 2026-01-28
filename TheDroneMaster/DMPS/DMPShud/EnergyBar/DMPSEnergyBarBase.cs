using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPShud.EnergyBar
{
    internal class DMPSEnergyBarBase
    {
        public static Vector2 pipSizeFull = new Vector2(2f, 20f);
        public static Vector2 pipSizeHalf = new Vector2(2f, 10f);

        public static float pipGap = 1f;


        public FContainer Container { get; private set; }
        public Vector2 pos, lastPos,anchorBias, lastAnchorBias;
        public Vector2 anchor = new Vector2(0f, 0.5f);

        //params
        public float lastAlpha, alpha;
        public float currentEnergy;
        float _show, _setShow;
        public float expand;

        public float Show
        {
            get => _show;
            set
            {
                _setShow = DMHelper.EaseInOutCubic(Mathf.Clamp01(value));
            }
        }
        float RealShow => Show * pips.Count;
        
        //show red params
        public float showRedProg, lastShowRedProg, setShowRed;
        public float redEnergy;
        public bool ShowRed
        {
            get => setShowRed > 0f;
            set => setShowRed = value ? 1f : 0f;
        }

        //show green params
        public float showGreenProg, lastShowGreenProg;
        public int GreenEnergy
        {
            get => greenPips.Count;
            set
            {
                if(value < greenPips.Count)
                {
                    for(int i = 0;i < greenPips.Count - value; i++)
                    {
                        greenPips[greenPips.Count - 1].RemoveSprites();
                        greenPips.RemoveAt(greenPips.Count - 1);
                    }
                }
                else if(value > greenPips.Count)
                {
                    for(int i = greenPips.Count;i < value; i++)
                    {
                        greenPips.Add(new Pip(this, i, true));
                    }
                }
            }
        }

        List<Pip> pips = new List<Pip>();
        List<Pip> greenPips = new List<Pip>();

        public float TotalWidth => pips.Count * (pipSizeFull.x + pipGap) - pipGap;

        public int TotalEnergy
        {
            get => pips.Count;
            set
            {
                if(value < pips.Count)
                {
                    for(int i = 0;i < pips.Count - value; i++)
                    {
                        pips[pips.Count - 1].RemoveSprites();
                        pips.RemoveAt(pips.Count - 1);
                    }
                }
                else if(value > pips.Count)
                {
                    for(int i = pips.Count;i < value; i++)
                    {
                        pips.Add(new Pip(this, i));
                    }
                }
            }
        }

        public DMPSEnergyBarBase(FContainer container)
        {
            Container = container;
        }

        public virtual void Update()
        {
            lastPos = pos;
            lastAlpha = alpha;
            lastAnchorBias = anchorBias;
            lastShowRedProg = showRedProg;
            showRedProg = Mathf.Lerp(showRedProg, setShowRed, 0.3f);

            anchorBias = Vector2.Lerp(anchorBias, new Vector2(-TotalWidth * anchor.x, pipSizeFull.y * (0.5f - anchor.y)), 0.15f);//默认以0,0.5为锚点绘制
            _show = Custom.LerpAndTick(_show, _setShow,0.25f, 1 / 80f);
            foreach (var pip in pips)
                pip.Update();
            foreach(var pip in greenPips)
                pip.Update();
        }

        public virtual void GrafUpdate(float timeStacker)
        {
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            float smoothRedShow = Mathf.Lerp(lastShowRedProg, showRedProg, timeStacker);
            float smoothGreenShow = Mathf.Lerp(lastShowGreenProg, showGreenProg, timeStacker);

            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            Vector2 smoothAnchorBias = Vector2.Lerp(lastAnchorBias, anchorBias, timeStacker);

            foreach (var pip in pips)
                pip.DrawSprites(timeStacker, smoothPos + smoothAnchorBias, smoothAlpha, smoothRedShow, smoothGreenShow);
            foreach (var pip in greenPips)
                pip.DrawSprites(timeStacker, smoothPos + smoothAnchorBias, smoothAlpha, smoothRedShow, smoothGreenShow);
        }

        public virtual void RemoveSprites()
        {
            foreach(var pip in pips)
                pip.RemoveSprites();
            foreach(var pip in greenPips)
                pip.RemoveSprites();
            pips.Clear();
            greenPips.Clear();
        }

        public class Pip
        {
            DMPSEnergyBarBase owner;
            FSprite bkgPip, lightPip, redMark;
            int index;
            
            float energyPercentage, lastEnergyPercentage;
            float showProg, lastShowProg, expandProg, lastExpandProg;
            float redExpand, lastRedExpand;

            bool green;
            int GreenIndex => owner.TotalEnergy + index;
            int Index
            {
                get
                {
                    if (green)
                        return GreenIndex;
                    else
                        return index;
                }
            }
            int ReverseIndex
            {
                get
                {
                    if (green)
                    {
                        return owner.pips.Count + owner.greenPips.Count - 1 - Index;
                    }
                    else
                    {
                        return owner.pips.Count - 1 - Index;
                    }
                }
            }


            public Pip(DMPSEnergyBarBase owner, int index, bool green = false)
            {
                this.owner = owner;
                this.index = index;
                this.green = green;

                bkgPip = new FSprite("pixel", true)
                {
                    scaleX = DMPSEnergyBarBase.pipSizeFull.x,
                    scaleY = DMPSEnergyBarBase.pipSizeFull.y,
                    color = green ? Color.green * 0.7f : StaticColors.Menu.darkPink,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    alpha = 1f,
                    isVisible = true
                };
                owner.Container.AddChild(bkgPip);

                lightPip = new FSprite("pixel", true)
                {
                    scaleX = DMPSEnergyBarBase.pipSizeFull.x,
                    scaleY = DMPSEnergyBarBase.pipSizeFull.y,
                    color = StaticColors.Menu.pink,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    alpha = 1f,
                    isVisible = !green
                };
                owner.Container.AddChild(lightPip);

                redMark = new FSprite("DMPS_PixelGradiant20", true)
                {
                    anchorY = 1f,//reverse
                    scaleX = DMPSEnergyBarBase.pipSizeFull.x + pipGap,
                    scaleY = 0.5f,
                    color = Color.red,
                    rotation = 180f,
                    shader = Custom.rainWorld.Shaders["AdditiveDefault"],
                    isVisible = !green
                };
                owner.Container.AddChild(redMark);
            }

            public virtual void Update()
            {
                lastEnergyPercentage = energyPercentage;
                energyPercentage = Mathf.Lerp(energyPercentage, Mathf.Clamp01(owner.currentEnergy - Index), 0.3f);

                lastShowProg = showProg;
                showProg = Mathf.Lerp(showProg, DMHelper.EaseInOutCubic(Mathf.Clamp01(owner.RealShow - Index)), 0.25f);

                lastExpandProg = expandProg;
                expandProg = Mathf.Lerp(expandProg, owner.expand, 0.2f);

                lastRedExpand = redExpand;
                float notShowRed = (owner.currentEnergy - Index - owner.redEnergy) > 0 ? 1f : 0f;
                redExpand = Mathf.Lerp(redExpand, Mathf.Clamp01(energyPercentage - notShowRed), 0.2f);
            }

            public virtual void DrawSprites(float timeStacker, Vector2 basePos, float alpha, float redShow, float greenShow)
            {
                float smoothEnergyPercentage = Mathf.Lerp(lastEnergyPercentage, energyPercentage, timeStacker);
                float smoothScaleY = Mathf.Lerp(pipSizeHalf.y, pipSizeFull.y, Mathf.Lerp(lastExpandProg, expandProg, timeStacker)) * Mathf.Lerp(lastShowProg, showProg, timeStacker);

                float smoothRedExpand = Mathf.Lerp(lastRedExpand, redExpand, timeStacker) * redShow;

                Vector2 pipPos = basePos + new Vector2(Index * (pipSizeFull.x + pipGap), 0f);
                bkgPip.SetPosition(pipPos);
                bkgPip.scaleY = smoothScaleY;
                bkgPip.alpha = alpha;

                lightPip.SetPosition(pipPos);
                lightPip.scaleY = smoothScaleY * smoothEnergyPercentage;
                lightPip.alpha = alpha;

                redMark.SetPosition(pipPos + new Vector2(0f, pipSizeFull.y * 0.7f));
                redMark.scaleY = 0.5f * smoothRedExpand;
                redMark.alpha = smoothRedExpand * alpha;
            }

            public void RemoveSprites()
            {
                owner.Container.RemoveChild(bkgPip);
                owner.Container.RemoveChild(lightPip);
                owner.Container.RemoveChild(redMark);
            }
        }
    }
}
