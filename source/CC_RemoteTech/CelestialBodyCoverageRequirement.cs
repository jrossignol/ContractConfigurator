using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using RemoteTech;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// ContractRequirement to check whether a celestial body has coverage.
    /// </summary>
    public class CelestialBodyCoverageRequirement : ContractRequirement
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Do not check on active contracts
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            LoggingUtil.LogVerbose(this, "Checking requirement");
            return RemoteTechProgressTracker.Instance.HasCoverage(targetBody);
        }
    }
}
