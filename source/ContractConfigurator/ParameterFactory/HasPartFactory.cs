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
     * ParameterFactory wrapper for HasPart ContractParameter.
     */
    public class HasPartFactory : ParameterFactory
    {
        protected int minCount { get; set; }
        protected int maxCount { get; set; }
        protected AvailablePart part { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minCount
            if (configNode.HasValue("minCount"))
            {
                minCount = Convert.ToInt32(configNode.GetValue("minCount"));
            }
            else
            {
                minCount = 1;
            }

            // Get maxCount
            if (configNode.HasValue("maxCount"))
            {
                maxCount = Convert.ToInt32(configNode.GetValue("maxCount"));
            }
            else
            {
                maxCount = int.MaxValue;
            }

            // Get part
            if (!configNode.HasValue("part"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'part'.");
            }
            else
            {
                part = ConfigNodeUtil.ParsePart(configNode, "part");
                valid &= part != null;
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasPart(part, minCount, maxCount, title);
        }
    }
}
