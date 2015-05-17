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
    /// ContractRequirement for performing an orbital survey of a celestial body.
    /// </summary>
    public class PerformOrbitalSurveyRequirement : ContractRequirement
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            if (ResourceScenario.Instance == null)
            {
                return false;
            }

            return ResourceScenario.Instance.gameSettings.GetPlanetScanInfo().Where(psd => psd.PlanetId == targetBody.flightGlobalsIndex).Any();
        }
    }
}
