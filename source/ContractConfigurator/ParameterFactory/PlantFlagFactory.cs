using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for PlantFlag ContractParameter.
    /// </summary>
    public class PlantFlagFactory : ParameterFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PlantFlag(targetBody);
        }
    }
}
