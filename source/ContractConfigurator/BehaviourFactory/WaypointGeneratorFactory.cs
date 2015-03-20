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
        private WaypointGenerator current;
        public WaypointGenerator Current
        {
            get
            {
                if (HighLogic.CurrentGame == null)
                {
                    return null;
                }

                if (current == null)
                {
                    current = (WaypointGenerator)Generate(null);
                }
                return current;
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
            WaypointGenerator result;
            if (current != null)
            {
                result = current;
                current = null;
                result.SetContract(contract);
            }
            else
            {
                result = new WaypointGenerator(waypointGeneratorTemplate, contract);
            }

            return result;
        }
    }
}
