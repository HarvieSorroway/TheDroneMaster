using CustomSaveTx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSave;
using UnityEngine;

namespace TheDroneMaster.DMPS.PlayerHooks
{
    public class DMPSBioReactor
    {
        DMPSBasicSave save;

        public static readonly float hypothermia2energyRatio = 2f;
        public static readonly float food2energyRatio = 4f;
        public float lowEnergyLim = 30;
        public int maxReactorEnergy => save.MaxEnergy;
        public float reactorEnergy
        {   
            get => save.Energy;
            set => save.Energy = value;
        }


        public float EnergyPercentage => Mathf.InverseLerp(0f, lowEnergyLim, reactorEnergy);
        public bool Chargeable => reactorEnergy < maxReactorEnergy;

        public DMPSBioReactor(Player player)
        {
            save = DeathPersistentSaveDataRx.GetTreatmentOfType<DMPSBasicSave>();
   
            //reactorEnergy = save.Energy;
            lowEnergyLim = Mathf.CeilToInt(maxReactorEnergy * 0.3f);
        }

        public void HypothermiaUpdate(Player player)
        {
            if (player.Hypothermia > 0f && reactorEnergy > 0f)
            {
                reactorEnergy = Mathf.Max(0f, reactorEnergy - player.Hypothermia * hypothermia2energyRatio);
                player.Hypothermia = 0f;
            }
        }

        public bool TrySpendEnergy(float spent)
        {
            if(reactorEnergy > spent)
            {
                reactorEnergy = Mathf.Max(0, reactorEnergy - spent);
                return true;
            }
            return false;
        }

        public void Charge(float charge)
        {
            reactorEnergy = Mathf.Min(maxReactorEnergy, reactorEnergy + charge);
        }
    }
}
