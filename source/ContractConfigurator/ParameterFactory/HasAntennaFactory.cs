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
    /// ParameterFactory wrapper for HasAntenna ContractParameter.
    /// </summary>
    public class HasAntennaFactory : ParameterFactory
    {
        protected double minAntennaPower;
        protected double maxAntennaPower;
		protected HasAntenna.AntennaType antennaType;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minAntennaPower", x => minAntennaPower = x, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxAntennaPower", x => maxAntennaPower = x, this, double.MaxValue, x => Validation.GE(x, 0.0f));
			valid &= ConfigNodeUtil.ParseValue<HasAntenna.AntennaType>(configNode, "antennaType", x => antennaType = x, this, HasAntenna.AntennaType.TRANSMIT);
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minAntennaPower", "maxAntennaPower" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasAntenna(minAntennaPower, maxAntennaPower, antennaType, title);
        }
    }
}
