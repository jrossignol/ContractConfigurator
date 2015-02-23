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
    /// ParameterFactory wrapper for HasResource ContractParameter.
    /// </summary>
    public class HasResourceFactory : ParameterFactory
    {
        protected double minQuantity;
        protected double maxQuantity;
        protected PartResourceDefinition resource;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minQuantity", x => minQuantity = x, this, 0.01, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxQuantity", x => maxQuantity = x, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<PartResourceDefinition>(configNode, "resource", x => resource = x, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasResource(resource, minQuantity, maxQuantity, title);
        }
    }
}
