using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Special placeholder for factories that failed to load.
    /// </summary>
    public class InvalidBehaviourFactory : BehaviourFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            base.Load(configNode);
            return false;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            throw new InvalidOperationException("Cannot generate invalid behaviour.");
        }
    }
}
