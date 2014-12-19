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
     * ParameterFactory wrapper for CollectScience ContractParameter.
     */
    public class CollectScienceFactory : ParameterFactory
    {
        protected BodyLocation location { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get location
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "location", this);
            try
            {
                string locationStr = configNode.GetValue("location");
                location = (BodyLocation)Enum.Parse(typeof(BodyLocation), locationStr);
            }
            catch (Exception e)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": error parsing location: " + e.Message);
            }

            // Validate target body
            if (targetBody == null)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody for CollectScience must be specified.");
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new CollectScience(targetBody, location);
        }
    }
}
