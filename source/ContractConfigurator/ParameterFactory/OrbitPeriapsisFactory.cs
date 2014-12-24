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
    public class OrbitPeriapsisFactory : ParameterFactory
    {
        protected double minPeriapsis { get; set; }
        protected double maxPeriapsis { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minInclination
            if (configNode.HasValue("minPeA"))
            {
                minPeriapsis = Convert.ToDouble(configNode.GetValue("minPeA"));
            }
            else
            {
                minPeriapsis = 0;
            }

            // Get maxPeriapsis
            if (configNode.HasValue("maxPeA"))
            {
                maxPeriapsis = Convert.ToDouble(configNode.GetValue("maxPeA"));
            }
            else
            {
                maxPeriapsis = int.MaxValue;
            }
            // Get minPeriapsis
            if (minPeriapsis == 0 && maxPeriapsis == int.MaxValue)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": max and min periapsis not given!");
            }

            if (targetBody == null)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": targetBody must be specified.");
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitPeriapsis(minPeriapsis, maxPeriapsis, targetBody, title);
        }
    }
}
