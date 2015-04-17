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
    /// BehaviourFactory wrapper for OrbitGenerator ContractBehaviour.
    /// </summary>
    public class OrbitGeneratorFactory : BehaviourFactory
    {
        OrbitGenerator orbitGenerator;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call SpawnKerbal for load behaviour
            orbitGenerator = OrbitGenerator.Create(configNode, targetBody, this);

            return valid && orbitGenerator != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new OrbitGenerator(orbitGenerator, contract);
        }
    }
}
