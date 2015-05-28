using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;
namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for UnlockTech ContractBehaviour.
    /// Author: Klefenz
    /// </summary>
    public class UnlockTechFactory : BehaviourFactory
    {
        protected List<string> techID;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "techID", x => techID = x, this);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new UnlockTech(techID);
        }
    }
}
