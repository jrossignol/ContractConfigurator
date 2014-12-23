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

            // Check on active contracts too
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : true;

            // Get Part
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "part", this);
            if (valid)
            {
                part = ConfigNodeUtil.ParsePart(configNode, "part");
                valid &= part != null;
            }

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ResearchAndDevelopment.PartTechAvailable(part);
        }
    }
}
