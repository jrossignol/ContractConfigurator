using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// ParameterFactory wrapper for HasAntennaParameter ContractParameter.
    /// </summary>
    public class HasAntennaFactory : ParameterFactory
    {
        protected int minCount;
        protected int maxCount;
        protected bool activeVessel;
        protected double minRange;
        protected double maxRange;
        protected HasAntennaParameter.AntennaType? antennaType;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", ref minCount, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", ref maxCount, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "activeVessel", ref activeVessel, this, false);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minRange", ref minRange, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxRange", ref maxRange, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<HasAntennaParameter.AntennaType?>(configNode, "antennaType", ref antennaType, this, (HasAntennaParameter.AntennaType?)null);
            valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "activeVessel" }, new string[] { "targetBody" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            HasAntennaParameter param = new HasAntennaParameter(minCount, maxCount, targetBody, activeVessel, antennaType, minRange, maxRange, title);

            return param;
        }
    }
}
