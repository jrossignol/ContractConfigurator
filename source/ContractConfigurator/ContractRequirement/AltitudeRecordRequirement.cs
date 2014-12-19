using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /*
     * ContractRequirement to provide requirement for player having reached a minimum altitude.
     */
    public class AltitudeRecordRequirement : ContractRequirement
    {
        protected double minAltitude { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get minAltitude
            if (!configNode.HasValue("minAltitude"))
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": missing required value 'minAltitude'.");
            }
            else
            {
                minAltitude = Convert.ToDouble(configNode.GetValue("minAltitude"));
                if (minAltitude <= 0.0)
                {
                    valid = false;
                    LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                        ": minAltitude must be greater than zero.");
                }
            }

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ProgressTracking.Instance.altitudeRecords.record > minAltitude;
        }
    }
}
