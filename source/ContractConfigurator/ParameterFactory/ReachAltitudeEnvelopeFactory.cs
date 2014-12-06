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
            if (!configNode.HasValue("minAltitude"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'minAltitude'.");
            }
            else if (Convert.ToDouble(configNode.GetValue("minAltitude")) <= 0.0d)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("minAltitude") + " for minAltitude.  Must be a real number greater than zero.");
            }
            minAltitude = (float)Convert.ToDouble(configNode.GetValue("minAltitude"));

            // Get maxAltitude
            if (!configNode.HasValue("maxAltitude"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'maxAltitude'.");
            }
            else if (Convert.ToDouble(configNode.GetValue("maxAltitude")) <= 0.0d)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("maxAltitude") + " for maxAltitude.  Must be a real number greater than zero.");
            }
            maxAltitude = (float)Convert.ToDouble(configNode.GetValue("maxAltitude"));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachAltitudeEnvelopeCustom(minAltitude, maxAltitude, title);
        }
    }
}
