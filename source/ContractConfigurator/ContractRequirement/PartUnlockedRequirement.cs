using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /*
     * ContractRequirement to provide requirement for player having unlocked a particular part.
     */
    public class PartUnlockedRequirement : ContractRequirement
    {
        protected AvailablePart part { get; set; }

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
                part = PartLoader.getPartInfoByName(configNode.GetValue("part"));
                if (part == null)
                {
                    valid = false;
                    LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                        ": invalid name for part: '" + configNode.GetValue("part") + "'.");
                }
            }

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ResearchAndDevelopment.PartTechAvailable(part);
        }
    }
}
