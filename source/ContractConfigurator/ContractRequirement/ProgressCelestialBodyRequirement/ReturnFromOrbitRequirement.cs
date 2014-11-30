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
     * ContractRequirement to provide requirement for player having performed returned from an
     * orbit of a specific CelestialBody.
     */
    public class ReturnFromOrbitRequirement : ProgressCelestialBodyRequirement
    {
        public override bool RequirementMet(ContractType contractType)
        {
            return base.RequirementMet(contractType) &&
                GetCelestialBodySubtree().returnFromOrbit.IsComplete;
        }
    }
}
