using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator
{
    /*
     * ParameterFactory for a paramter for reaching a specific orbit.
     */
    public class ReachSpecificOrbitFactory : ParameterFactory
    {
        protected int index { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get orbit details from the OrbitGenerator behaviour
            index = configNode.HasValue("index") ? Convert.ToInt32(configNode.GetValue("index")) : 0;

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            // Get the OrbitGenerator behaviour
            OrbitGenerator orbitGenerator = ((ConfiguredContract)contract).Behaviours.OfType<OrbitGenerator>().First<OrbitGenerator>();

            if (orbitGenerator == null)
            {
                LoggingUtil.LogError(this, "Could not find OrbitGenerator BEHAVIOUR to couple with ReachSpecificOrbit PARAMETER.");
                return null;
            }

            // Get the parameter for that orbit
            try
            {
                SpecificOrbitWrapper s = orbitGenerator.GetOrbitParameter(index);
                return new VesselParameterDelegator(s);
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Couldn't find orbit in OrbitGenerator with index " + index + ": " + e.Message);
                return null;
            }
        }
    }
}
