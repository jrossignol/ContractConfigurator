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
    /// ContractRequirement to provide requirement for player having performed returned from the
    /// surface of a specific CelestialBody.
    /// </summary>
    public class ReturnFromSurfaceRequirement : ProgressCelestialBodyRequirement
    {
        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // This appears bugged - returnFromSurface is null
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().returnFromSurface.IsComplete;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have returned from " + ACheckTypeString() + "landing on " + targetBody.theName;

            return output;
        }
    }
}
