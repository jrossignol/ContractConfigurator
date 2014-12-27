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
        protected AvailablePart part;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Check on active contracts too
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : true;

            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", ref part, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ResearchAndDevelopment.PartTechAvailable(part);
        }
    }
}
