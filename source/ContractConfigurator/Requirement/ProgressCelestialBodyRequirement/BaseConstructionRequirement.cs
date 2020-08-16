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
    /// ContractRequirement to provide requirement for player having built a base on a specific CelestialBody.
    /// </summary>
    public class BaseConstructionRequirement : ProgressCelestialBodyRequirement
    {
		protected override ProgressNode GetTypeSpecificProgressNode(CelestialBodySubtree celestialBodySubtree)
		{
            return celestialBodySubtree.baseConstruction;
		}

		public override bool RequirementMet(ConfiguredContract contract)
        {
            return base.RequirementMet(contract) &&
                GetCelestialBodySubtree().IsComplete;
        }
    }
}
