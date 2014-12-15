using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator.Behaviour
{
    /*
     * BehaviourFactory wrapper for Expression ContractBehaviour.
     */
    public class ExpressionFactory : BehaviourFactory
    {
        Expression expression;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call SpawnKerbal for load behaviour
            expression = Expression.Parse(configNode);

            return valid && expression != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new Expression(expression);
        }
    }
}
