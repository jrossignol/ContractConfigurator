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
    public class ReachAltitudeEnvelopeFactory : ParameterFactory
    {
        protected float minAltitude { get; set; }
        protected float maxAltitude { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minAltitude
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "minAltitude", this);
            if (valid && Convert.ToDouble(configNode.GetValue("minAltitude")) <= 0.0d)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("minAltitude") + " for minAltitude.  Must be a real number greater than zero.");
            }
            else
            {
                minAltitude = (float)Convert.ToDouble(configNode.GetValue("minAltitude"));
            }

            // Get maxAltitude
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "maxAltitude", this);
            if (valid && Convert.ToDouble(configNode.GetValue("maxAltitude")) <= 0.0d)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("maxAltitude") + " for maxAltitude.  Must be a real number greater than zero.");
            }
            else
            {
                maxAltitude = (float)Convert.ToDouble(configNode.GetValue("maxAltitude"));
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachAltitudeEnvelopeCustom(minAltitude, maxAltitude, title);
        }
    }
}
