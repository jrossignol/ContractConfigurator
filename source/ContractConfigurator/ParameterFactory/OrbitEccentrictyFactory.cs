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
    public class OrbitEccentricityFactory : ParameterFactory
    {
        protected double minEccentricity { get; set; }
        protected double maxEccentricity { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minEccentricity
            if (configNode.HasValue("minEccentricity"))
            {
                minEccentricity = Convert.ToDouble(configNode.GetValue("minEccentricity"));
            }
            else
            {
                minEccentricity = 0;
            }

            // Get maxEccentricity
            if (configNode.HasValue("maxEccentricity"))
            {
                maxEccentricity = Convert.ToDouble(configNode.GetValue("maxEccentricity"));
            }
            else
            {
                maxEccentricity = int.MaxValue;
            }

            if (minEccentricity == 0 && maxEccentricity == int.MaxValue)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": max and min Eccentricity not given!");
            }

            if (minEccentricity < 0)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": minvalue is out bound! (< 0)");
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
            return new OrbitEccentricity(minEccentricity, maxEccentricity, targetBody, title);
        }
    }
}
