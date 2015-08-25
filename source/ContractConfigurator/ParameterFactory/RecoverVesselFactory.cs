using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for RecoverVessel ContractParameter.
    /// </summary>
    public class RecoverVesselFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            return new RecoverVessel(title);
        }
    }
}
