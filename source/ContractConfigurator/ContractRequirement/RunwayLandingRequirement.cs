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
     * ContractRequirement to provide requirement for player having landed on the runway.
     */
    public class RunwayLandingRequirement : ContractRequirement
    {
        public override bool RequirementMet(ContractType contractType)
        {
            return ProgressTracking.Instance.runwayLanding.IsComplete;
        }
    }
}
