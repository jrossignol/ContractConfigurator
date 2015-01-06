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
     * ParameterFactory wrapper for OrbitAltitude ContractParameter.
     */
    public class OrbitPeriodFactory : ParameterFactory
    {
        protected double minPeriod;
        protected double maxPeriod;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minPeriod
            string minPeriodStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "minPeriod", ref minPeriodStr, this, "");
            if (minPeriodStr != null)
            {
                minPeriod = DurationUtil.ParseDuration(minPeriodStr);
            }

            // Get maxPeriod
            string maxPeriodStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "maxPeriod", ref maxPeriodStr, this, "");
            if (maxPeriodStr != null)
            {
                maxPeriod = DurationUtil.ParseDuration(maxPeriodStr);
            }

            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitPeriod(minPeriod, maxPeriod, targetBody, title);
        }
    }
}
