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
        protected VesselIdentifier vesselKey;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            if (configNode.HasValue("vesselKey"))
            {
                LoggingUtil.LogWarning(this, "The 'vesselKey' attribute is obsolete as of Contract Configurator 0.7.5.  It will be removed in 1.0.0 in favour of the vessel attribute.");
                valid &= ConfigNodeUtil.ParseValue<VesselIdentifier>(configNode, "vesselKey", x => vesselKey = x, this);
            }
            else
            {
                valid &= ConfigNodeUtil.ParseValue<VesselIdentifier>(configNode, "vessel", x => vesselKey = x, this);
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new IsNotVessel(vesselKey.identifier, title);
        }
    }
}
