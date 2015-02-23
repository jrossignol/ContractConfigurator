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
        protected List<AvailablePart> parts;
        protected bool add;
        protected bool remove;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => parts = x, this);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "add", x => add = x, this, true);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "remove", x => remove = x, this, true);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new ExperimentalPart(parts, add, remove);
        }
    }
}
