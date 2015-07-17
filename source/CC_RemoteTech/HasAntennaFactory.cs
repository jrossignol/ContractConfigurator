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
            bool hasTargetBody = configNode.HasValue("targetBody");

            // Load base class
            bool valid = base.Load(configNode);

            // Base class attempts to load a default, remove it and re-load
            if (!hasTargetBody)
            {
                configNode.RemoveValue("targetBody");
                valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", x => _targetBody = x, this, (CelestialBody)null);
            }

            // Before loading, verify the RemoteTech version
            valid &= Util.Version.VerifyRemoteTechVersion();

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", x => minCount = x, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", x => maxCount = x, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "activeVessel", x => activeVessel = x, this, false);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minRange", x => minRange = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxRange", x => maxRange = x, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<HasAntennaParameter.AntennaType?>(configNode, "antennaType", x => antennaType = x, this, (HasAntennaParameter.AntennaType?)null);
            valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "activeVessel" }, new string[] { "targetBody" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            HasAntennaParameter param = new HasAntennaParameter(minCount, maxCount, _targetBody, activeVessel, antennaType, minRange, maxRange, title);

            return param;
        }
    }
}
