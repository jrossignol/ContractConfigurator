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
     * ParameterFactory wrapper for OrbitInclination ContractParameter.
     */
    [Obsolete("Obsolete, use Orbit")]
    public class OrbitInclinationFactory : ParameterFactory
    {
        protected double minInclination;
        protected double maxInclination;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minInclination", ref minInclination, this, 0.0, x => Validation.Between(x, 0.0, 180.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxInclination", ref maxInclination, this, 180.0, x => Validation.Between(x, 0.0, 180.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minInclination", "maxInclination" }, this);
            valid &= ValidateTargetBody(configNode);

            LoggingUtil.LogError(this, ErrorPrefix() + ": OrbitInclination is obsolete as of ContractConfigurator 0.5.0, please use Orbit instead.  OrbitInclination will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitInclination(minInclination, maxInclination, targetBody, title);
        }
    }
}
