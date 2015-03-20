using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Generic parser subclass for behaviour factories.  Placeholder for expressions that are common to behaviours.
    /// </summary>
    public class BehaviourParser<T> : ClassExpressionParser<T> where T : BehaviourFactory
    {
        public BehaviourParser()
        {
        }
    }
}
