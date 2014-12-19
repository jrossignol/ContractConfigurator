using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;

namespace ContractConfigurator
{
    /*
     * ContractRequirement that executes an expression.
     */
    public class ExpressionRequirement : ContractRequirement
    {
        protected string expression { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get expression
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "expression", this);
            if (valid)
            {
                expression = configNode.GetValue("expression");
                ExpressionParser.ParseExpression(expression);
            }
            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ExpressionParser.ExecuteExpression(expression) != 0.0;
        }
    }
}
