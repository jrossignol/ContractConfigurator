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
    /// BehaviourFactory wrapper for SpawnKerbal ContractBehaviour.
    /// </summary>
    public class SpawnKerbalFactory : BehaviourFactory
    {
        protected SpawnKerbal spawnKerbalTemplate;
        public SpawnKerbal Current
        {
            get
            {
                if (HighLogic.CurrentGame == null)
                {
                    return null;
                }

                spawnKerbalTemplate.Initialize();
                return spawnKerbalTemplate;
            }
        } 

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Call SpawnKerbal for load behaviour
            spawnKerbalTemplate = SpawnKerbal.Create(configNode, targetBody, this);

            return valid && spawnKerbalTemplate != null;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new SpawnKerbal(spawnKerbalTemplate);
        }
    }
}
