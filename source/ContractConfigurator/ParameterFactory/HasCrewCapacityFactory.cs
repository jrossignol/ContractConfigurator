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
    /// ParameterFactory wrapper for HasCrewCapacity ContractParameter.
    /// </summary>
    public class HasCrewCapacityFactory : ParameterFactory
    {
        protected int minCapacity;
        protected int maxCapacity;
        protected PartResourceDefinition resource;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCapacity", x => minCapacity = x, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCapacity", x => maxCapacity = x, this, int.MaxValue, x => Validation.GE(x, 0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasCrewCapacity(minCapacity, maxCapacity, title);
        }
    }
}
