using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using KSP.Localization;

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
            return ProgressTracking.Instance.altitudeRecords.record >= minAltitude;
        }

        protected override string RequirementText()
        {
            string output = Localizer.Format(invertRequirement ? "#cc.req.AltitudeRecord.x" : "#cc.req.AltitudeRecord", minAltitude.ToString("N0"));

            if (ProgressTracking.Instance.altitudeRecords.record < minAltitude)
            {
                output = Localizer.Format("#cc.req.AltitudeRecord.additional", output, ProgressTracking.Instance.altitudeRecords.record.ToString("N0"));
            }

            return output;
        }
    }
}
