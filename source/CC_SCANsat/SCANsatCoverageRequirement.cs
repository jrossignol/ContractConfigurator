using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using SCANsat;

namespace ContractConfigurator.SCANsat
{
    /*
     * ContractRequirement for SCANsat coverage.
     */
    public class SCANsatCoverageRequirement : ContractRequirement
    {
        protected string scanType;
        protected double minCoverage;
        protected double maxCoverage;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Do not check the requirement on active contracts.  Otherwise when they scan the
            // contract is invalidated, which is usually not what's meant.
            checkOnActiveContract = false;

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minCoverage", ref minCoverage, this, 0.0);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxCoverage", ref maxCoverage, this, 100.0);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "scanType", ref scanType, this, SCANsatUtil.ValidateSCANname);
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            double coverageInPercentage = SCANUtil.GetCoverage(SCANUtil.GetSCANtype(scanType), targetBody);
            return coverageInPercentage >= minCoverage && coverageInPercentage <= maxCoverage;
        }
    }
}
