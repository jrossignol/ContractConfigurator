using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /*
     * ContractRequirement to provide requirement for player having performed returned from the
     * surface of a specific CelestialBody.
     */
    public class ReturnFromSurfaceRequirement : ProgressCelestialBodyRequirement
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Validate targetBody
            if (targetBody.name.Equals("Kerbin"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody cannot be Kerbin for ReturnFromSurface.");
            }

            return valid;
        }

        public override bool RequirementMet(ContractType contractType)
        {
            // This appears bugged - returnFromSurface is null
            return base.RequirementMet(contractType) &&
                GetCelestialBodySubtree().returnFromSurface.IsComplete;
        }
    }
}
