using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for ExperimentalPart ContractBehaviour.
    /// </summary>
    public class ExperimentalPartFactory : BehaviourFactory
    {
        protected AvailablePart part;
        protected bool add;
        protected bool remove;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", ref part, this);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "add", ref add, this, true);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "remove", ref remove, this, true);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new ExperimentalPart(part, add, remove);
        }
    }
}
