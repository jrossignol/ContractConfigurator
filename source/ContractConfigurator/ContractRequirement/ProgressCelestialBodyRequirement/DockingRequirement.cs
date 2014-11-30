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
     * ContractRequirement to provide requirement for player having performed docking near a
     * specific CelestialBody.
     */
    public class DockingRequirement : ProgressCelestialBodyRequirement
    {
        public override bool RequirementMet(ContractType contractType)
        {
            return base.RequirementMet(contractType) &&
                GetCelestialBodySubtree().docking.IsComplete;
        }
    }
}
