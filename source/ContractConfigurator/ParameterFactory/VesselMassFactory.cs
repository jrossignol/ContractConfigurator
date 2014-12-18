using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for VesselMass ContractParameter.
     */
    public class VesselMassFactory : ParameterFactory
    {
        protected float minMass { get; set; }
        protected float maxMass { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minMass
            if (configNode.HasValue("minMass"))
            {
                minMass = (float)Convert.ToDouble(configNode.GetValue("minMass"));
            }
            else
            {
                minMass = 0.0f;
            }

            // Get maxMass
            if (configNode.HasValue("maxMass"))
            {
                maxMass = (float)Convert.ToDouble(configNode.GetValue("maxMass"));
            }
            else
            {
                maxMass = float.MaxValue;
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselMass(minMass, maxMass, title);
        }
    }
}
