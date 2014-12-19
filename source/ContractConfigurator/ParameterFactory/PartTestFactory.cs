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
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": missing required value 'part'.");
            }
            else
            {
                part = ConfigNodeUtil.ParsePart(configNode, "part");
                valid &= part != null;
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
