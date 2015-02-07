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
    public class KSCConnectivityFactory : ParameterFactory
    {
        protected bool hasConnectivity;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Before loading, verify the RemoteTech version
            valid &= Util.VerifyRemoteTechVersion();

            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hasConnectivity", ref hasConnectivity, this, true);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new KSCConnectivityParameter(hasConnectivity, title);
        }
    }
}
