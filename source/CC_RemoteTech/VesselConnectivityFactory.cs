using ContractConfigurator.Parameters;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using UnityEngine;
using RemoteTech;

namespace ContractConfigurator.RemoteTech
{
    public class VesselConnectivityFactory : ParameterFactory
    {
        protected bool hasConnectivity;
        protected string vesselKey;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hasConnectivity", ref hasConnectivity, this, true);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "vesselKey", ref vesselKey, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselConnectivity(vesselKey, hasConnectivity, title);
        }
    }
}
