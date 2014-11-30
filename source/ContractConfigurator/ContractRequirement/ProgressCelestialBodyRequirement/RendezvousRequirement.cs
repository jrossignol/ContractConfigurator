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
     * ContractRequirement to provide requirement for player having rendezvoued with a
     * specific CelestialBody.
     */
    public class RendezvousRequirement : ProgressCelestialBodyRequirement
    {
        public override bool RequirementMet(ContractType contractType)
        {
            return base.RequirementMet(contractType) &&
                GetCelestialBodySubtree().rendezvous.IsComplete;
        }
    }
}
