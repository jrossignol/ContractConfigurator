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
        protected double minAltitude;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minAltitude", ref minAltitude, this, x => Validation.GT(x, 0.0));

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ProgressTracking.Instance.altitudeRecords.record > minAltitude;
        }
    }
}
