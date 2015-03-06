using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having unlocked a particular part.
    /// </summary>
    public class PartUnlockedRequirement : ContractRequirement
    {
        protected AvailablePart part;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Do not check on active contracts.
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", x => part = x, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ResearchAndDevelopment.PartTechAvailable(part);
        }
    }
}
