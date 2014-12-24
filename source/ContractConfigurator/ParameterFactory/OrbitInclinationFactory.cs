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
    public class OrbitInclinationFactory : ParameterFactory
    {
        protected double minInclination { get; set; }
        protected double maxInclination { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minInclination
            if (configNode.HasValue("minInclination"))
            {
                minInclination = Convert.ToDouble(configNode.GetValue("minInclination"));
            }
            else
            {
                minInclination = 0;
            }

            // Get maxInclination
            if (configNode.HasValue("maxInclination"))
            {
                maxInclination = Convert.ToDouble(configNode.GetValue("maxInclination"));
            }
            else
            {
                maxInclination = 180;
            }

            if (!configNode.HasValue("minInclination") && !configNode.HasValue("maxInclination"))
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": either minInclination or maxInclination must be supplied!");
            }

            if (minInclination < 0 || maxInclination > 180)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": min or max value is out bound! (< 0 or > 180)");
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
            return new OrbitInclination(minInclination, maxInclination, targetBody, title);
        }
    }
}
