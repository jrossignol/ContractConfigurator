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
    /// BehaviourFactory wrapper for SpawnPassengers ContractBehaviour.
    /// </summary>
    public class SpawnPassengersFactory : BehaviourFactory
    {
        protected int count;
        protected List<string> passengerName;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "count", x => count = x, this, 1, x => Validation.GE(x, 1));
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "passengerName", x => passengerName = x, this, new List<string>());

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new SpawnPassengers(passengerName, count);
        }
    }
}
