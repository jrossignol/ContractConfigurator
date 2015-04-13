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
    /// ParameterFactory wrapper for VesselNotDestroyed ContractParameter. 
    /// </summary>
    public class VesselNotDestroyedFactory : ParameterFactory
    {
        protected List<VesselIdentifier> vessels;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<VesselIdentifier>>(configNode, "vessel", x => vessels = x, this, new List<VesselIdentifier>());

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselNotDestroyed(vessels.Select<VesselIdentifier, string>(vi => vi.identifier), title);
        }
    }
}
