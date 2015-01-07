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
    public class OrbitApoapsisFactory : ParameterFactory
    {
        protected double minApA;
        protected double maxApA;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minApA", ref minApA, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxApA", ref maxApA, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minApA", "maxApA" }, this);
            valid &= ValidateTargetBody(configNode);

            LoggingUtil.LogError(this, "OrbitApoapsis is obsolete as of ContractConfigurator 0.5.0, please use Orbit instead.  OrbitApoapsis will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitApoapsis(minApA, maxApA, targetBody, title);
        }
    }
}
