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
    /// ContractRequirement to provide requirement for player having a certain amount of reputation.
    /// </summary>
    public class ReputationRequirement : ContractRequirement
    {
        protected float minReputation;
        protected float maxReputation;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minReputation", x => minReputation = x, this, -1000.0f, x => Validation.Between(x, -1000.0f, 1000.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxReputation", x => maxReputation = x, this, 1000.0f, x => Validation.Between(x, -1000.0f, 1000.0f));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minReputation", "maxReputation" }, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("minReputation", minReputation);
            configNode.AddValue("maxReputation", maxReputation);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            minReputation = ConfigNodeUtil.ParseValue<float>(configNode, "minReputation");
            maxReputation = ConfigNodeUtil.ParseValue<float>(configNode, "maxReputation");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            float reputation = Reputation.Instance.reputation;
            return reputation >= minReputation && reputation <= maxReputation;
        }
    }
}
