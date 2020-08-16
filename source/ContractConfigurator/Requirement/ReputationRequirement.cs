using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using KSP.Localization;

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

            // Not invertable
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false, x => Validation.EQ(x, false));

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

        protected override string RequirementText()
        {
            if (minReputation > -1000 && maxReputation < 1000)
            {
                return Localizer.Format("#cc.req.Reputation.between", minReputation.ToString("N0"), maxReputation.ToString("N0"));
            }
            else if (minReputation > -1000)
            {
                return Localizer.Format("#cc.req.Reputation.atLeast", minReputation.ToString("N0"));
            }
            else
            {
                return Localizer.Format("#cc.req.Reputation.atMost", maxReputation.ToString("N0"));
            }
        }
    }
}
