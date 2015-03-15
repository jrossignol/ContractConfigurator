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
    /// <summary>
    /// ParameterFactory wrapper for VesselDestroyed ContractParameter. 
    /// </summary>
    public class VesselDestroyedFactory : ParameterFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselDestroyed(title);
        }
    }
}
