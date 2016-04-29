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
    /// ContractRequirement to provide requirement for player having a certain amount of funds.
    /// </summary>
    public class FundsRequirement : ContractRequirement
    {
        protected double minFunds;
        protected double maxFunds;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minFunds", x => minFunds = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxFunds", x => maxFunds = x, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minFunds", "maxFunds" }, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("minFunds", minFunds);
            if (maxFunds != double.MaxValue)
            {
                configNode.AddValue("maxFunds", maxFunds);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            minFunds = ConfigNodeUtil.ParseValue<double>(configNode, "minFunds");
            maxFunds = ConfigNodeUtil.ParseValue<double>(configNode, "maxFunds", double.MaxValue);
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            double funds = Funding.Instance.Funds;
            return funds >= minFunds && funds <= maxFunds;
        }
    }
}
