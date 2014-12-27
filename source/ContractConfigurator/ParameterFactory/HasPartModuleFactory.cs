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
     * ParameterFactory wrapper for HasPartModule ContractParameter.
     */
    public class HasPartModuleFactory : ParameterFactory
    {
        protected int minCount;
        protected int maxCount;
        protected string partModule;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", ref minCount, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", ref maxCount, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "partModule", ref partModule, this, Validation.ValidatePartModule);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasPartModule(partModule, minCount, maxCount, title);
        }
    }
}
