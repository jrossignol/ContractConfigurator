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
     * ParameterFactory wrapper for ReachSpeedEnvelope ContractParameter.
     */
    public class ReachSpeedEnvelopeFactory : ParameterFactory
    {
        protected float minSpeed { get; set; }
        protected float maxSpeed { get; set; }
        protected string title { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minSpeed
            if (!configNode.HasValue("minSpeed"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'minSpeed'.");
            }
            else if (Convert.ToDouble(configNode.GetValue("minSpeed")) <= 0.0d)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("minSpeed") + " for minSpeed.  Must be a real number greater than zero.");
            }
            minSpeed = (float)Convert.ToDouble(configNode.GetValue("minSpeed"));

            // Get maxSpeed
            if (!configNode.HasValue("maxSpeed"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'maxSpeed'.");
            }
            else if (Convert.ToDouble(configNode.GetValue("maxSpeed")) <= 0.0d)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("maxSpeed") + " for maxSpeed.  Must be a real number greater than zero.");
            }
            maxSpeed = (float)Convert.ToDouble(configNode.GetValue("maxSpeed"));

            // Get title
            title = configNode.HasValue("title") ? configNode.GetValue("title") :
                "Speed: Between " + minSpeed.ToString("N0") + " and " + maxSpeed.ToString("N0") + " m/s";

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            ReachSpeedEnvelope parameter = new ReachSpeedEnvelope(maxSpeed, minSpeed, title);
            parameter.useSpdLimits = false;
            return parameter;
        }
    }
}
