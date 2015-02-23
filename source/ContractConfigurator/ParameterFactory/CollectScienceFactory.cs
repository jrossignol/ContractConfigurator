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
    /// ParameterFactory wrapper for CollectScience ContractParameter.
    /// </summary>
    public class CollectScienceFactory : ParameterFactory
    {
        protected BodyLocation location;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<BodyLocation>(configNode, "location", x => location = x, this);
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new CollectScience(targetBody, location);
        }
    }
}
