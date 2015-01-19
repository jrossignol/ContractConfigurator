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
     * ParameterFactory wrapper for OrbitApoapsis ContractParameter.
     */
    [Obsolete("Obsolete, use Orbit")]
    public class OrbitPeriapsisFactory : ParameterFactory
    {
        protected double minPeA;
        protected double maxPeA;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minPeA", ref minPeA, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxPeA", ref maxPeA, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minPeA", "maxPeA" }, this);
            valid &= ValidateTargetBody(configNode);

            LoggingUtil.LogError(this, ErrorPrefix() + ": OrbitPeriapsis is obsolete as of ContractConfigurator 0.5.0, please use Orbit instead.  OrbitPeriapsis will be removed in a future release.");
            
            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitPeriapsis(minPeA, maxPeA, targetBody, title);
        }
    }
}
