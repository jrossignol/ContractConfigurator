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

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get expression
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "expression", x => expression = x, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Always met once offered (as the expression at that point may have been changed by another contract
            return expression || contract.ContractState == Contract.State.Offered;
        }
    }
}
