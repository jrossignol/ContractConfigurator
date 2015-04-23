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
    /// ParameterFactory wrapper for AtLeast ContractParameter.
    /// </summary>
    public class AtLeastFactory : ParameterFactory
    {
        protected int count;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "count", x => count = x, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new AtLeast(title, count);
        }
    }
}
