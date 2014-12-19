using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for EnterOrbit ContractParameter.
     */
    public class EnterOrbitFactory : ParameterFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Validate target body
            if (targetBody == null)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": targetBody for EnterOrbit must be specified.");
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new EnterOrbit(targetBody);
        }
    }
}
