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
     * ParameterFactory wrapper for PartTest ContractParameter.
     */
    public class PartTestFactory : ParameterFactory
    {
        protected AvailablePart part { get; set; }
        protected string notes { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get Part
            if (!configNode.HasValue("part"))
            {
                valid = false;
                part = null;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'part'.");
            }
            else
            {
                part = PartLoader.getPartInfoByName(configNode.GetValue("part"));
                if (part == null)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                        ": invalid name for part: '" + configNode.GetValue("part") + "'.");
                }
            }

            // Get notes
            notes = configNode.HasValue("notes") ? configNode.GetValue("notes") : null;

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PartTest(part, notes);
        }
    }
}
