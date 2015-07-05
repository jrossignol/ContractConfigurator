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
    /// BehaviourFactory wrapper for SpawnVessel ContractBehaviour.
    /// </summary>
    public class SpawnVesselFactory : BehaviourFactory
    {
        SpawnVessel spawnVessel;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call SpawnKerbal for load behaviour
            spawnVessel = SpawnVessel.Create(configNode, this);

            return valid && spawnVessel != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new SpawnVessel(spawnVessel);
        }
    }
}
