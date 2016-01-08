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
    /// BehaviourFactory wrapper for RemoveKerbal ContractBehaviour.
    /// </summary>
    public class RemoveKerbalFactory : BehaviourFactory
    {
        protected List<Kerbal> kerbals;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<Kerbal>>(configNode, "kerbal", x => kerbals = x, this);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new RemoveKerbalBehaviour(kerbals);
        }
    }
}
