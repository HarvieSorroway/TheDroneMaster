using DMPS.PlayerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSDynamicParams;
using TheDroneMaster.DMPS.DMPShud.EnergyBar;
using UnityEngine;
using static TheDroneMaster.DMPS.DMPSDynamicParams.DMPSDynamicParams;

namespace TheDroneMaster.DMPS.PlayerHooks.BioReactors
{
    internal class OverDriveReactor : DMPSBioReactor, IDMPSDynamicParam
    {
        public static float maxOverDriveCapacity = 10f;
        public static float overDriveDropPerSec = 0.2f;

        float overDriveEnergy;

        public bool OverDrive => overDriveEnergy > 0;

        public string ID => "OverDriveReactor";

        OverDriveMessage OverDriveMessage => message as OverDriveMessage;

        public OverDriveReactor(Player player, DMPSModule module) : base(player, module)
        {
            DynamicParamInstance.RegisterDynamicParam(this);
        }

        public override void SetMessage(DMPSModule module)
        {
            module.energyBarMessage = message = new OverDriveMessage();
        }

        public override void Update(Player player)
        {
            base.Update(player);
            if(OverDrive)
                overDriveEnergy = Mathf.Max(0f, overDriveEnergy - overDriveDropPerSec / 40f);
            OverDriveMessage.overDriveEnergy = overDriveEnergy;
        }

        public override bool TrySpendEnergy(float spent)
        {
            if(OverDrive)
            {
                overDriveEnergy = Mathf.Max(0, overDriveEnergy - spent * DynamicParamInstance.CostMultiplier);
                return true;
            }

            return base.TrySpendEnergy(spent);
        }

        public override void Charge(float charge)
        {
            float d = Mathf.Max(0f, reactorEnergy + charge - maxReactorEnergy);

            if (d > 0f)
                overDriveEnergy = Mathf.Max(overDriveDropPerSec, overDriveEnergy + d);
            reactorEnergy = Mathf.Min(maxReactorEnergy, reactorEnergy + charge);
        }

        public bool GetParam(DynamicParamType paramType, out float param)
        {
            if (paramType == DynamicParamType.CostMultiplier)
            {
                param = OverDrive ? 0.5f : 1f;
                return true;
            }
            else if (paramType == DynamicParamType.DmgMultiplier)
            {
                param = OverDrive ? 1.5f : 1f;
                return true;
            }
            else
            {
                param = 0f;
                return false;
            }
        }
    }
}
