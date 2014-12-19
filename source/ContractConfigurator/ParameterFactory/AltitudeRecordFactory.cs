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
    /*
     * ParameterFactory wrapper for AltitudeRecord ContractParameter.
     */
    public class AltitudeRecordFactory : ParameterFactory
    {
        protected double altitude { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get altitude
            if (!configNode.HasValue("altitude"))
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": missing required value 'altitude'.");
            }
            else if (Convert.ToDouble(configNode.GetValue("altitude")) <= 0.0f)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": invalid value of " + configNode.GetValue("altitude") + " for altitude.  Must be a real number greater than zero.");
            }
            altitude = Convert.ToDouble(configNode.GetValue("altitude"));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new AltitudeRecord(altitude);
        }
    }
}
