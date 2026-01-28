using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheDroneMaster.DMPS.PlayerHooks
{
    internal class DMPSDynamicParams
    {
        List<IDMPSDynamicParam> dmgMultiplierParams = new List<IDMPSDynamicParam>();
        public float DynamicDmgMultiplier { get; private set; }

        public void Update()
        {

        }

        public void RegisterDmgMultiplierParam(IDMPSDynamicParam param)
        {
            dmgMultiplierParams.Add(param);
        }

        public interface IDMPSDynamicParam
        {
            float GetParam();
        }
    }
}
