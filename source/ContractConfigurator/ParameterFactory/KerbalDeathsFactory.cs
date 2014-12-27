using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for KerbalDeaths ContractParameter.  Also, if you need a KSP themed
     * band name, I think Kerbal Death Factory seems suitable...
     */
    public class KerbalDeathsFactory : ParameterFactory
    {
        protected int countMax;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "countMax", ref countMax, this, 1, x => Validation.GT(x, 0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new KerbalDeaths(countMax);
        }
    }
}
