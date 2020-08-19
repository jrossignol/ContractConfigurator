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
    /// ContractRequirement for performing an orbital survey of a celestial body.
    /// </summary>
    public class PerformOrbitalSurveyRequirement : ContractRequirement
    {
        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return false;
            }

            if (ResourceScenario.Instance == null)
            {
                return false;
            }

            return ResourceScenario.Instance.gameSettings.GetPlanetScanInfo().Where(psd => psd.PlanetId == targetBody.flightGlobalsIndex).Any();
        }

        public override void OnLoad(ConfigNode configNode) { }
        public override void OnSave(ConfigNode configNode) { }

        protected override string RequirementText()
        {
            string body = targetBody == null ? Localizer.GetStringByTag("#cc.req.ProgressCelestialBody.genericBody") : targetBody.displayName;
            return Localizer.Format(invertRequirement ? "#cc.req.PerformOrbitalSurvey.x" : "#cc.req.PerformOrbitalSurvey", body);
        }
    }
}
