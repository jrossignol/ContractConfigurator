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
    /*
     * ParameterFactory wrapper for HasPassengers ContractParameter.
     */
    public class HasPassengersFactory : ParameterFactory
    {
        protected int count;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "count", ref count, this, 1, x => Validation.GE(x, 1));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasPassengers(title, count);
        }
    }
}
