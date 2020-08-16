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

            // Not invertable
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false, x => Validation.EQ(x, false));

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

        protected override string RequirementText()
        {
            if (minFunds > 0 && maxFunds < double.MaxValue)
            {
                return Localizer.Format("#cc.req.Funds.between", minFunds.ToString("N0"), maxFunds.ToString("N0"));
            }
            else if (minFunds > 0)
            {
                return Localizer.Format("#cc.req.Funds.atLeast", minFunds.ToString("N0"));
            }
            else
            {
                return Localizer.Format("#cc.req.Funds.atMost", maxFunds.ToString("N0"));
            }
        }
    }
}
