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
    /// ParameterFactory wrapper for None ContractParameter.
    /// </summary>
    public class NoneFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            return new None(title);
        }
    }
}
