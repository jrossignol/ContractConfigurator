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
    /// <summary>
    /// Special placeholder for factories that failed to load.
    /// </summary>
    public class InvalidParameterFactory : ParameterFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            base.Load(configNode);
            return false;
        }

        public override ContractParameter Generate(Contract contract)
        {
            throw new InvalidOperationException("Cannot generate invalid parameter.");
        }
    }
}
