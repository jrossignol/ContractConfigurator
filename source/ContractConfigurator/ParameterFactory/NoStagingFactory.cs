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
    /// ParameterFactory wrapper for NoStaging ContractParameter.
    /// </summary>
    public class NoStagingFactory : ParameterFactory
    {
        protected bool failContract;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "failContract", x => failContract = x, this, true);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new NoStaging(failContract, title);
        }
    }
}
