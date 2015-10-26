using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for ReachSpace ContractParameter.
    /// </summary>
    [Obsolete("ReachSpace is obsolete since Contract Configurator 1.7.7, use ReachState instead.")]
    public class ReachSpaceFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            LoggingUtil.LogWarning(this, "ReachSpace is obsolete since Contract Configurator 1.7.7, use ReachState instead.");
            return new ReachSpaceCustom(title);
        }
    }
}
