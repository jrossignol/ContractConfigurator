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
    /// ContractRequirement to provide requirement for player having landed on the runway.
    /// </summary>
    public class RunwayLandingRequirement : ContractRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ProgressTracking.Instance.runwayLanding.IsComplete;
        }

        public override void OnLoad(ConfigNode configNode) { }
        public override void OnSave(ConfigNode configNode) { }
    }
}
