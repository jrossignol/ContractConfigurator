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
    public class OrbitApoapsisFactory : ParameterFactory
    {
        protected double minApoapsis { get; set; }
        protected double maxApoapsis { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minApoapsis
            if (configNode.HasValue("minApA"))
            {
                minApoapsis = Convert.ToDouble(configNode.GetValue("minApA"));
            }
            else
            {
                minApoapsis = 0;
            }

            // Get maxApoapsis
            if (configNode.HasValue("maxApA"))
            {
                maxApoapsis = Convert.ToDouble(configNode.GetValue("maxApA"));
            }
            else
            {
                maxApoapsis = double.MaxValue;
            }

            if (!configNode.HasValue("minApA") && !configNode.HasValue("maxApA"))
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": either minApA or maxApA must be supplied!");
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
            return new OrbitApoapsis(minApoapsis, maxApoapsis, targetBody, title);
        }
    }
}
