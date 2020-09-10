using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using ContractConfigurator.Util;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement set requirement.  Requirement is met if any child requirement is met.
    /// </summary>
    public class AnyRequirement : ContractRequirement
    {
        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            bool requirementMet = false;
            foreach (ContractRequirement requirement in childNodes)
            {
                if (requirement.enabled)
                {
                    requirementMet |= requirement.CheckRequirement(contract);
                }
            }
            return requirementMet;
        }

        public override void OnLoad(ConfigNode configNode) { }
        public override void OnSave(ConfigNode configNode) { }

        protected override string RequirementText()
        {
            return Localizer.Format(invertRequirement ? "#cc.req.Any.x" : "#cc.req.Any", MissionControlUI.RequirementHighlightColor);
        }
    }
}
