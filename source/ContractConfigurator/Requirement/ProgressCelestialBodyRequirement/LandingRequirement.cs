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
    /// ContractRequirement to provide requirement for player having landed on a specific CelestialBody.
    /// </summary>
    public class LandingRequirement : ProgressCelestialBodyRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().landing.IsComplete;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have performed " + ACheckTypeString() + "landing on " + (targetBody == null ? "the target body" : targetBody.theName);

            return output;
        }
    }
}
