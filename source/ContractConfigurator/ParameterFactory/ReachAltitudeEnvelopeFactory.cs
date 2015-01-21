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
     * ParameterFactory wrapper for ReachAltitudeEnvelope ContractParameter.
     */
    [Obsolete("Obsolete, use ReachState")]
    public class ReachAltitudeEnvelopeFactory : ParameterFactory
    {
        protected float minAltitude;
        protected float maxAltitude;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minAltitude", ref minAltitude, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxAltitude", ref maxAltitude, this, float.MaxValue, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "maxAltitude", "maxAltitude" }, this);

            LoggingUtil.LogError(this, ErrorPrefix() + ": ReachAltitudeEnvelope is obsolete as of ContractConfigurator 0.5.3, please use ReachState instead.  ReachAltitudeEnvelope will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachAltitudeEnvelopeCustom(minAltitude, maxAltitude, title);
        }
    }
}
