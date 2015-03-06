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
    /// ParameterFactory wrapper for VesselIsType ContractParameter.
    /// </summary>
    public class VesselIsTypeFactory : ParameterFactory
    {
        protected VesselType vesselType;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<VesselType>(configNode, "vesselType", x => vesselType = x, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselIsType(vesselType, title);
        }
    }
}
