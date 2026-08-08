using DMPS.PlayerHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPShud.EnergyBar;
using UnityEngine;
using static TheDroneMaster.DMPS.DMPSDynamicParams.DMPSDynamicParams;

namespace TheDroneMaster.DMPS.PlayerHooks.BioReactors
{
    internal class FeedbackReactor : DMPSBioReactor
    {
        const float energyToEfficiencyRate = 0.03f, //每消耗1点能量，反馈效率预计提升的数量
                    efficiencyDropPerFrame = 0.05f / 40f, 
                    efficientyDropMinPerFrame = 0.01f / 40f,
                    framesToDropPerEnergy = 0.5f; 
 

        float feedBackEfficiency = 0f;

        public float FeedBackEfficiency
        {
            get => feedBackEfficiency;
            set => feedBackEfficiency = Mathf.Clamp01(value);
        }

        public float CostDiscount => Mathf.Lerp(1f, 0.1f, FeedBackEfficiency);

        float efficiencyStorage, pluseStack;
        int framesToDrop;

        FeedBackMessage FeedBackMessage => message as FeedBackMessage;
        public FeedbackReactor(Player player, DMPSModule module) : base(player, module)
        {
        }
        public override void SetMessage(DMPSModule module)
        {
            module.energyBarMessage = message = new FeedBackMessage();
        }
        public override bool TrySpendEnergy(float spent)
        {
            bool res = base.TrySpendEnergy(spent * CostDiscount);

            if (res)
            {
                efficiencyStorage += spent * energyToEfficiencyRate;
                framesToDrop += Mathf.FloorToInt(efficiencyStorage * framesToDropPerEnergy);
                pluseStack += spent;
            }

            return res;
        }

        public override void Update(Player player)
        {
            base.Update(player);
            if (framesToDrop > 0f)
            {
                framesToDrop--;
            }
            else if(efficiencyStorage > 0)
            {
                float drop = Mathf.Min(FeedBackEfficiency, Mathf.Clamp(efficiencyDropPerFrame * FeedBackEfficiency, efficientyDropMinPerFrame, efficiencyDropPerFrame));
                FeedBackEfficiency -= drop;
            }

            if(efficiencyStorage > 0)
            {
                float add = Mathf.Min(efficiencyStorage, efficiencyDropPerFrame);
                FeedBackEfficiency += add;
                efficiencyStorage = Mathf.Max(0f, efficiencyStorage - add);
            }


            FeedBackMessage.feedBackEfficiency = FeedBackEfficiency;
            FeedBackMessage.pluseStack = pluseStack;
            pluseStack = 0f;
        }
    }
    internal class FeedBackMessage : EnergyBarMessage
    {
        public float feedBackEfficiency;
        public float pluseStack;
    }
}
