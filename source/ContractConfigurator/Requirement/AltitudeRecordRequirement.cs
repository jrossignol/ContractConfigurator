using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having reached a minimum altitude.
    /// </summary>
    public class AltitudeRecordRequirement : ContractRequirement
    {
        protected double minAltitude;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minAltitude", x => minAltitude = x, this, x => Validation.GT(x, 0.0));

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {

            configNode.AddValue("minAltitude", minAltitude);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            minAltitude = ConfigNodeUtil.ParseValue<double>(configNode, "minAltitude");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ProgressTracking.Instance.altitudeRecords.record > minAltitude;
        }
    }
}
