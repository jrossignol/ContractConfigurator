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
     * ContractRequirement to provide requirement for player having a certain amount of reputation.
     */
    public class ReputationRequirement : ContractRequirement
    {
        protected float minReputation;
        protected float maxReputation;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minReputation", ref minReputation, this, -1000.0f, x => Validation.Between(x, -1000.0f, 1000.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxReputation", ref maxReputation, this, 1000.0f, x => Validation.Between(x, -1000.0f, 1000.0f));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minReputation", "maxReputation" }, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            float reputation = Reputation.Instance.reputation;
            return reputation >= minReputation && reputation <= maxReputation;
        }
    }
}
