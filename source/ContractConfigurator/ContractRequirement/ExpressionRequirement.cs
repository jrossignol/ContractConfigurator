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
            if (!configNode.HasValue("expression"))
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": missing required value 'expression'.");
            }
            else
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
