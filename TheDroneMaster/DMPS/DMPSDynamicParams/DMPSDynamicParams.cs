using CustomSaveTx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheDroneMaster.DMPS.DMPSSave;
using UnityEngine;

namespace TheDroneMaster.DMPS.DMPSDynamicParams
{
    internal class DMPSDynamicParams
    {
        public static DMPSDynamicParams DynamicParamInstance { get; private set; }

        DMPSBasicSave save;

        List<IDMPSDynamicParam> dynamicParams = new List<IDMPSDynamicParam>();

        public float DmgMultiplier { get; private set; } = 1f;
        public float DroneDmgMultiplier { get; private set; } = 1f;


        public float CostMultiplier { get; private set; } = 1f;

        public DMPSDynamicParams(DMPSBasicSave save)
        {
            this.save = save;
        }

        public void Update()
        {
            DmgMultiplier = 1f + dynamicParams.Sum((i) =>
            {
                if (i.GetParam(DynamicParamType.DmgMultiplier, out float param))
                    return param;
                else
                    return 0f;
            });

            DroneDmgMultiplier = 1f + dynamicParams.Sum((i) =>
            {
                if (i.GetParam(DynamicParamType.DroneDmgMultiplier, out float param))
                    return param;
                else
                    return 0f;
            }) * DmgMultiplier;

            CostMultiplier = 1f;
            foreach (var dynamicP in dynamicParams)
            {
                if (dynamicP.GetParam(DynamicParamType.CostMultiplier, out float param))
                    CostMultiplier *= param;
            }
        }

        public void RegisterDynamicParam(IDMPSDynamicParam param)
        {
            foreach(var kvp in dynamicParams)
            {
                if (kvp.ID == param.ID)
                    return;
            }
            dynamicParams.Add(param);
        }

        public static void New()
        {
            DynamicParamInstance = new DMPSDynamicParams(DeathPersistentSaveDataRx.GetTreatmentOfType<DMPSBasicSave>());
        }

        public interface IDMPSDynamicParam
        {
            public string ID { get; }
            bool GetParam(DynamicParamType paramType, out float param);
        }

        public enum DynamicParamType
        {
            DmgMultiplier,
            DroneDmgMultiplier,
            CostMultiplier
        }
    }
}
