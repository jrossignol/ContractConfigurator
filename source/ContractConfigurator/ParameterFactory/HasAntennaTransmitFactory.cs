using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for HasAntennaTransmit ContractParameter.
    /// </summary>
    public class HasAntennaTransmitFactory : ParameterFactory
    {
        protected double minAntennaPower;
        protected double maxAntennaPower;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minAntennaPower", x => minAntennaPower = x, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxAntennaPower", x => maxAntennaPower = x, this, double.MaxValue, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minAntennaPower", "maxAntennaPower" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasAntennaTransmit(minAntennaPower, maxAntennaPower, title);
        }
    }
}
