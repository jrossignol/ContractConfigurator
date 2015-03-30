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
    /// <summary>
    /// ParameterFactory to provide logic for Sequence.
    /// </summary>
    public class SequenceFactory : ParameterFactory
    {
        protected List<string> hiddenParameters;
        protected bool failWhenCompleteOutOfOrder;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "hiddenParameter", x => hiddenParameters = x, this, new List<string>());
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "failWhenCompleteOutOfOrder", x => failWhenCompleteOutOfOrder = x, this, false);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.Sequence(hiddenParameters, failWhenCompleteOutOfOrder, title);
        }
    }
}
