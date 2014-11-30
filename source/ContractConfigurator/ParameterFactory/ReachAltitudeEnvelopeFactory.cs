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
    /*
     * ParameterFactory wrapper for ReachAltitudeEnvelope ContractParameter.
     */
    public class ReachAltitudeEnvelopeFactory : ParameterFactory
    {
        protected float minAltitude { get; set; }
        protected float maxAltitude { get; set; }
        protected string title { get; set; }

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

            // Get title
            title = configNode.HasValue("title") ? configNode.GetValue("title") :
                "Altitude: Between " + minAltitude.ToString("N0") + " and " + maxAltitude.ToString("N0") + " meters";

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            ReachAltitudeEnvelope parameter = new ReachAltitudeEnvelope(maxAltitude, minAltitude, title);
            parameter.useAltLimits = false;
            return parameter;
        }
    }
}
