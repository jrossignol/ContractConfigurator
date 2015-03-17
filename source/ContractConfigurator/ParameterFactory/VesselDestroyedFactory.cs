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
        protected bool mustImpactTerrain = false;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "mustImpactTerrain", x => mustImpactTerrain = x, this, false, x => true);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselDestroyed(title, mustImpactTerrain);
        }
    }
}
