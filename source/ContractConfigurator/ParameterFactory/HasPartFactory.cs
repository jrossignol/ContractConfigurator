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
     * ParameterFactory wrapper for HasPart ContractParameter.
     */
    [Obsolete("Obsolete, use PartValidationFactory")]
    public class HasPartFactory : ParameterFactory
    {
        protected int minCount;
        protected int maxCount;
        protected AvailablePart part;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", ref minCount, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", ref maxCount, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", ref part, this);

            LoggingUtil.LogError(this, "HasPart is obsolete as of ContractConfigurator 0.5.0, please use PartValidation instead.  HasPart will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasPart(part, minCount, maxCount, title);
        }
    }
}
