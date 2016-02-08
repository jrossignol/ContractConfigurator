using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for KerbalDeaths ContractParameter.
    /// </summary>
    public class KerbalDeathsFactory : ParameterFactory
    {
        protected int countMax;
        protected List<Kerbal> kerbal;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "countMax", x => countMax = x, this, 1, x => Validation.GT(x, 0));
            valid &= ConfigNodeUtil.ParseValue<List<Kerbal>>(configNode, "kerbal", x => kerbal = x, this, new List<Kerbal>());

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new KerbalDeathsCustom(countMax, kerbal, title);
        }
    }
}
