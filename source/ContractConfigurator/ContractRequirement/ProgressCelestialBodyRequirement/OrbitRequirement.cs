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
     * ContractRequirement to provide requirement for player having orbited a
     * specific CelestialBody.
     */
    public class OrbitRequirement : ProgressCelestialBodyRequirement
    {
        public override bool RequirementMet(ContractType contractType)
        {
            return base.RequirementMet(contractType) &&
                GetCelestialBodySubtree().orbit.IsComplete;
        }
    }
}
