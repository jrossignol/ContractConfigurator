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
    public class ReachSpaceFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            return new ReachSpaceCustom(title);
        }
    }
}
