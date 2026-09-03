using CustomSaveTx;
using DMPS.PlayerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSDynamicParams;
using TheDroneMaster.DMPS.DMPSSave;
using UnityEngine;

namespace TheDroneMaster.DMPS.PlayerHooks.BioReactors
{
    internal class DMPSBioReactor : PlayerModule.PlayerModuleUtil
    {
        DMPSBasicSave save;

        public static readonly float hypothermia2energyRatio = 2f;
        public static readonly float food2energyRatio = 4f;
        public float lowEnergyLim = 30;
        public int maxReactorEnergy => save.SkillData.MaxEnergy;
        public float reactorEnergy
        {   
            get => save.Energy;
            set => save.Energy = value;
        }


        public float EnergyPercentage => Mathf.InverseLerp(0f, lowEnergyLim, reactorEnergy);
        public bool Chargeable => reactorEnergy < maxReactorEnergy;

        protected DMPShud.EnergyBar.EnergyBarMessage message;

        public DMPSBioReactor(Player player, DMPSModule module)
        {
            save = DeathPersistentSaveDataRx.GetTreatmentOfType<DMPSBasicSave>();

            SetMessage(module);
            //reactorEnergy = save.Energy;
            lowEnergyLim = Mathf.CeilToInt(maxReactorEnergy * 0.3f);
        }

        public virtual void SetMessage(DMPSModule module)
        {
            message = module.energyBarMessage = new DMPShud.EnergyBar.EnergyBarMessage();
        }

        public override void Update(Player player)
        {
            base.Update(player);
            message.totalEnergy = maxReactorEnergy;
            message.currentEnergy = reactorEnergy;
        }

        public virtual void HypothermiaUpdate(Player player)
        {
            if (player.Hypothermia > 0f && TrySpendEnergy(player.Hypothermia * hypothermia2energyRatio))
                player.Hypothermia = 0f;
        }

        public virtual bool TrySpendEnergy(float spent)
        {
            if(reactorEnergy > spent)
            {
                reactorEnergy = Mathf.Max(0, reactorEnergy - spent * DMPSDynamicParams.DMPSDynamicParams.DynamicParamInstance.CostMultiplier);
                return true;
            }
            return false;
        }

        public virtual void Charge(float charge)
        {
            reactorEnergy = Mathf.Min(maxReactorEnergy, reactorEnergy + charge);
        }
    }
}
