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
    /// ContractRequirement to provide requirement for player made their first launch.
    /// </summary>
    public class FirstLaunchRequirement : ContractRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ProgressTracking.Instance.firstLaunch.IsComplete;
        }

        public override void OnLoad(ConfigNode configNode) { }
        public override void OnSave(ConfigNode configNode) { }

        protected override string RequirementText()
        {
            return Localizer.GetStringByTag(invertRequirement ? "#cc.req.FirstLaunch.x" : "#cc.req.FirstLaunch");
        }
    }
}
