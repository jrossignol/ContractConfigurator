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
    /// <summary>
    /// ParameterFactory for a paramter for reaching a specific orbit.
    /// </summary>
    public class ReachSpecificOrbitFactory : ParameterFactory
    {
        protected int index;
        protected double deviationWindow;
        protected bool displayNotes;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get orbit details from the OrbitGenerator behaviour
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "displayNotes", x => displayNotes = x, this, true);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "deviationWindow", x => deviationWindow = x, this, 10.0, x => Validation.GE(x, 0.0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            // Get the OrbitGenerator behaviour
            OrbitGenerator orbitGenerator = ((ConfiguredContract)contract).Behaviours.OfType<OrbitGenerator>().FirstOrDefault<OrbitGenerator>();

            if (orbitGenerator == null)
            {
                LoggingUtil.LogError(this, "Could not find OrbitGenerator BEHAVIOUR to couple with ReachSpecificOrbit PARAMETER.");
                return null;
            }

            Orbit orbit = orbitGenerator.GetOrbit(index);
            if (orbit == null)
            {
                LoggingUtil.LogError(this, "Could not find orbit with index " + index + ".");
                return null;
            }

            return new OrbitParameter(orbit, deviationWindow, displayNotes);
        }
    }
}
