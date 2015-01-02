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
    /// ParameterFactory wrapper for IsNotVesselFactory ContractParameter. 
    /// </summary>
    public class IsNotVesselFactory : ParameterFactory
    {
        protected string vesselKey;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "vesselKey", ref vesselKey, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new IsNotVessel(vesselKey, title);
        }
    }
}
