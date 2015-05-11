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
        WaypointGenerator waypointGeneratorTemplate;
        public WaypointGenerator Current
        {
            get
            {
                return waypointGeneratorTemplate;
            }
        } 

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call SpawnKerbal for load behaviour
            waypointGeneratorTemplate = WaypointGenerator.Create(configNode, targetBody, this);

            return valid && waypointGeneratorTemplate != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new WaypointGenerator(waypointGeneratorTemplate, contract);
        }
    }
}
