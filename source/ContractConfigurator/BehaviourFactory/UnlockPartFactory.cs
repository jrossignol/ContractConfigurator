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
    /// BehaviourFactory wrapper for UnlockPart ContractBehaviour.
    /// </summary>
    public class UnlockPartFactory : BehaviourFactory
    {
        protected List<AvailablePart> parts;
        protected bool unlockTech;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => parts = x, this);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "unlockTech", x => unlockTech = x, this, true);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new UnlockPart(parts, unlockTech);
        }
    }
}
