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
     * ParameterFactory wrapper for ReachSpace ContractParameter.
     */
    public class ReachSpaceFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            return new ReachSpace();
        }
    }
}
