using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for RecoverKerbal ContractParameter.
    /// </summary>
    public class RecoverKerbalFactory : ParameterFactory
    {
        protected List<Kerbal> kerbal;
        protected int index;
        protected int count;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<Kerbal>>(configNode, "kerbal", x => kerbal = x, this, new List<Kerbal>());
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "count", x => count = x, this, 0, x => Validation.GE(x, 0));
            
            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new RecoverKerbalCustom(kerbal.Select<Kerbal, string>(k => k.name), index, count, title);
        }
    }
}
