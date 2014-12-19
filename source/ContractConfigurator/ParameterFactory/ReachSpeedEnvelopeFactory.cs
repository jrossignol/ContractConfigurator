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
    public class ReachSpeedEnvelopeFactory : ParameterFactory
    {
        protected double minSpeed { get; set; }
        protected double maxSpeed { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minSpeed
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "minSpeed", this);
            if (valid && Convert.ToDouble(configNode.GetValue("minSpeed")) <= 0.0d)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("minSpeed") + " for minSpeed.  Must be a real number greater than zero.");
            }
            else
            {
                minSpeed = (float)Convert.ToDouble(configNode.GetValue("minSpeed"));
            }

            // Get maxSpeed
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "maxSpeed", this);
            if (valid && Convert.ToDouble(configNode.GetValue("maxSpeed")) <= 0.0d)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("maxSpeed") + " for maxSpeed.  Must be a real number greater than zero.");
            }
            else
            {
                maxSpeed = (float)Convert.ToDouble(configNode.GetValue("maxSpeed"));
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachSpeedEnvelopeCustom(minSpeed, maxSpeed, title);
        }
    }
}
