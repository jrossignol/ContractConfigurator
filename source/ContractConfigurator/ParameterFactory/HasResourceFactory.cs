using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for HasResource ContractParameter.
     */
    public class HasResourceFactory : ParameterFactory
    {
        protected double minQuantity { get; set; }
        protected double maxQuantity { get; set; }
        protected PartResourceDefinition resource { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minQuantity
            if (configNode.HasValue("minQuantity"))
            {
                minQuantity = Convert.ToDouble(configNode.GetValue("minQuantity"));
            }
            else
            {
                minQuantity = 0.0;
            }

            // Get maxQuantity
            if (configNode.HasValue("maxQuantity"))
            {
                maxQuantity = Convert.ToDouble(configNode.GetValue("maxQuantity"));
            }
            else
            {
                maxQuantity = double.MaxValue;
            }

            // Get resource
            if (!configNode.HasValue("resource"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'resource'.");
            }
            else
            {
                resource = ConfigNodeUtil.ParseResource(configNode, "resource");
                valid &= resource != null;
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasResource(resource, minQuantity, maxQuantity, title);
        }
    }
}
