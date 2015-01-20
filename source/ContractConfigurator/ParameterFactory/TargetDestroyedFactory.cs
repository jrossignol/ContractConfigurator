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
    /// ParameterFactory wrapper for TargetDestroyed ContractParameter. 
    /// </summary>
    public class TargetDestroyedFactory : ParameterFactory
    {
        protected List<string> vessels;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", ref vessels, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new TargetDestroyed(vessels, title);
        }
    }
}
