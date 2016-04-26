using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement that executes an expression.
    /// </summary>
    public class ExpressionRequirement : ContractRequirement
    {
        protected bool expression;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Get expression
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "expression", x => expression = x, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("expression", expression);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            expression = ConfigNodeUtil.ParseValue<bool>(configNode, "expression");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return expression;
        }
    }
}
