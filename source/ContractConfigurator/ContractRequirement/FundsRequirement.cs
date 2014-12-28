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
     * ContractRequirement to provide requirement for player having a certain amount of funds.
     */
    public class FundsRequirement : ContractRequirement
    {
        protected double minFunds;
        protected double maxFunds;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minFunds", ref minFunds, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxFunds", ref maxFunds, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minFunds", "maxFunds" }, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            double funds = Funding.Instance.Funds;
            return funds >= minFunds && funds <= maxFunds;
        }
    }
}
