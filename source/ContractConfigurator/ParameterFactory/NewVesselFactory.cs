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
    /// ParameterFactory wrapper for the NewVessel ContractParameter.
    /// </summary>
    public class NewVesselFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            return new NewVessel(title);
        }
    }
}
