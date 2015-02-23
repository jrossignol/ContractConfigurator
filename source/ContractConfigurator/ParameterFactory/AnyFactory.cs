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
    /// ParameterFactory wrapper for OR ContractParameter.
    /// </summary>
    public class AnyFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.Any(title);
        }
    }
}
