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

            // Before loading, verify the RemoteTech version
            valid &= Util.VerifyRemoteTechVersion();

            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hasConnectivity", x => hasConnectivity = x, this, true);

            if (configNode.HasValue("vesselKey"))
            {
                LoggingUtil.LogWarning(this, "The 'vesselKey' attribute is obsolete as of Contract Configurator 0.7.8.  It will be removed in 1.0.0 in favour of the vessel attribute.");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "vesselKey", x => vesselKey = x, this);
            }
            else
            {
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "vessel", x => vesselKey = x, this);
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new VesselConnectivityParameter(vesselKey, hasConnectivity, title);
        }
    }
}
