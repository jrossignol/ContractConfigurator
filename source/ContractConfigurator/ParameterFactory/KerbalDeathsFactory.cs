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
    /// <summary>
    /// ParameterFactory wrapper for KerbalDeaths ContractParameter.
    /// </summary>
    public class KerbalDeathsFactory : ParameterFactory
    {
        protected int countMax;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "countMax", x => countMax = x, this, 1, x => Validation.GT(x, 0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new KerbalDeaths(countMax);
        }
    }
}
