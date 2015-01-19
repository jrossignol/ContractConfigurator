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
     * ParameterFactory wrapper for OrbitEccentricity ContractParameter.
     */
    [Obsolete("Obsolete, use Orbit")]
    public class OrbitEccentricityFactory : ParameterFactory
    {
        protected double minEccentricity;
        protected double maxEccentricity;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minEccentricity", ref minEccentricity, this, 0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxEccentricity", ref maxEccentricity, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minEccentricity", "maxEccentricity" }, this);
            valid &= ValidateTargetBody(configNode);

            LoggingUtil.LogError(this, ErrorPrefix() + ": OrbitEccentricity is obsolete as of ContractConfigurator 0.5.0, please use Orbit instead.  OrbitEccentricity will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitEccentricity(minEccentricity, maxEccentricity, targetBody, title);
        }
    }
}
