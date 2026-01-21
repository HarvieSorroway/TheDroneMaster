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
        public FContainer Container { get; private set; }
        public Vector2 pos, lastPos;

        public DMPSEnergyBarBase(FContainer container)
        {
            Container = container;
        }

        public virtual void Update()
        {

        }

        public virtual void GrafUpdate(float timeStacker)
        {

        }

        public virtual void RemoveSprites()
        {

        }

        public class Pip
        {
            FSprite bkgPip, lightPip;
            public Pip(DMPSEnergyBarBase owner)
            {

            }
        }
    }
}
