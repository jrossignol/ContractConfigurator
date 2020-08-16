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
    /// ContractRequirement to provide requirement for player having reached space.
    /// </summary>
    public class ReachSpaceRequirement : ContractRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ProgressTracking.Instance.reachSpace.IsComplete;
        }

        public override void OnLoad(ConfigNode configNode) { }
        public override void OnSave(ConfigNode configNode) { }

        protected override string RequirementText()
        {
            return Localizer.GetStringByTag(invertRequirement ? "#cc.req.ReachSpace.x" : "#cc.req.ReachSpace");
        }
    }
}
