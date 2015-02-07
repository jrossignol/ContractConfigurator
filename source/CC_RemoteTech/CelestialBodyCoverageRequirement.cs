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
        protected double minCoverage;
        protected double maxCoverage;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Before loading, verify the RemoteTech version
            valid &= Util.VerifyRemoteTechVersion();

            // Do not check on active contracts
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minCoverage", ref minCoverage, this, 0.0, x => Validation.BetweenInclusive(x, 0.0, 1.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxCoverage", ref maxCoverage, this, 0.0, x => Validation.BetweenInclusive(x, 0.0, 1.0));
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            LoggingUtil.LogVerbose(this, "Checking requirement");

            if (RemoteTechProgressTracker.Instance == null)
            {
                // Assume no coverage
                return maxCoverage == 0.0;
            }

            double coverage = RemoteTechProgressTracker.Instance.GetCoverage(targetBody);
            return coverage >= minCoverage && coverage <= maxCoverage;
        }
    }
}
