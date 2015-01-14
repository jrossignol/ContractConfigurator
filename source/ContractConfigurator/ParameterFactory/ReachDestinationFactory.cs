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
    /*
     * ParameterFactory wrapper for ReachDestination ContractParameter.
     */
    [Obsolete("Obsolete, use ReachState")]
    public class ReachDestinationFactory : ParameterFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ValidateTargetBody(configNode);

            LoggingUtil.LogError(this, "ReachDestination is obsolete as of ContractConfigurator 0.5.3, please use ReachState instead.  ReachDestination will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachDestinationCustom(targetBody, title);
        }
    }
}
