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
    /*
     * ParameterFactory wrapper for ReachSpeedEnvelope ContractParameter.
     */
    [Obsolete("Obsolete, use ReachState")]
    public class ReachSpeedEnvelopeFactory : ParameterFactory
    {
        protected double minSpeed;
        protected double maxSpeed;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minSpeed", ref minSpeed, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxSpeed", ref maxSpeed, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minSpeed", "maxSpeed" }, this);

            LoggingUtil.LogError(this, "ReachSpeedEnvelope is obsolete as of ContractConfigurator 0.5.3, please use ReachState instead.  ReachSpeedEnvelope will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachSpeedEnvelopeCustom(minSpeed, maxSpeed, title);
        }
    }
}
