using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for Expression ContractBehaviour.
    /// </summary>
    public class ExpressionFactory : BehaviourFactory
    {
        Expression expression;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call Expression for load behaviour
            try
            {
                expression = Expression.Parse(configNode, dataNode, this);
            }
            catch (Exception e)
            {
                valid = false;
                LoggingUtil.LogError(this, ErrorPrefix(configNode) + ": Couldn't load expression.");
                LoggingUtil.LogException(e);
            }

            return valid && expression != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new Expression(expression);
        }
    }
}
