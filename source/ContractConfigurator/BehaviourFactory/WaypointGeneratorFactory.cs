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
    /// BehaviourFactory wrapper for WaypointGenerator ContractBehaviour.
    /// </summary>
    public class WaypointGeneratorFactory : BehaviourFactory
    {
        WaypointGenerator waypointGenerator;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call SpawnKerbal for load behaviour
            waypointGenerator = WaypointGenerator.Create(configNode, targetBody, this);

            return valid && waypointGenerator != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new WaypointGenerator(waypointGenerator, contract);
        }
    }
}
