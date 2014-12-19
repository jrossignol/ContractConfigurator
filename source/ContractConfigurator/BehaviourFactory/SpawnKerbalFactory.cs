using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /*
     * BehaviourFactory wrapper for SpawnKerbal ContractBehaviour.
     */
    public class SpawnKerbalFactory : BehaviourFactory
    {
        SpawnKerbal spawnKerbal;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call SpawnKerbal for load behaviour
            spawnKerbal = SpawnKerbal.Create(configNode, targetBody, this);

            return valid && spawnKerbal != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new SpawnKerbal(spawnKerbal);
        }
    }
}
