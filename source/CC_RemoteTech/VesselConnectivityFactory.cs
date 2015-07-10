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
        protected string vessel;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Before loading, verify the RemoteTech version
            valid &= Util.Version.VerifyRemoteTechVersion();

            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hasConnectivity", x => hasConnectivity = x, this, true);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "vessel", x => vessel = x, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselConnectivityParameter(vessel, hasConnectivity, title);
        }
    }
}
