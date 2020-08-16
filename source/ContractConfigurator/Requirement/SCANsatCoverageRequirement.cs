using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement for SCANsat coverage.
    /// </summary>
    public class SCANsatCoverageRequirement : ContractRequirement
    {
        protected string scanType;
        protected double minCoverage;
        protected double maxCoverage;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Before loading, verify the SCANsat version
            if (!SCANsatUtil.VerifySCANsatVersion())
            {
                return false;
            }

            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Not invertable
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false, x => Validation.EQ(x, false));

            // Do not check the requirement on active contracts.  Otherwise when they scan the
            // contract is invalidated, which is usually not what's meant.
            checkOnActiveContract = false;

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minCoverage", x => minCoverage = x, this, 0.0);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxCoverage", x => maxCoverage = x, this, 100.0);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "scanType", x => scanType = x, this, SCANsatUtil.ValidateSCANname);
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("minCoverage", minCoverage);
            configNode.AddValue("maxCoverage", maxCoverage);
            configNode.AddValue("scanType", scanType);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            minCoverage = ConfigNodeUtil.ParseValue<double>(configNode, "minCoverage");
            maxCoverage = ConfigNodeUtil.ParseValue<double>(configNode, "maxCoverage");
            scanType = ConfigNodeUtil.ParseValue<string>(configNode, "scanType");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return false;
            }

            double coverageInPercentage = SCANsatUtil.GetCoverage(SCANsatUtil.GetSCANtype(scanType), targetBody);
            return coverageInPercentage >= minCoverage && coverageInPercentage <= maxCoverage;
        }

        protected override string RequirementText()
        {
            string body = targetBody == null ? Localizer.GetStringByTag("#cc.req.ProgressCelestialBody.genericBody") : targetBody.CleanDisplayName(true);
            if (minCoverage > 0 && maxCoverage < 100.0)
            {
                return Localizer.Format("#cc.scansat.req.SCANsatCoverage.between", minCoverage.ToString("N0"), maxCoverage.ToString("N0"), SCANsatCoverage.ScanDisplayName(scanType), body);
            }
            else if (minCoverage > 0)
            {
                return Localizer.Format("#cc.scansat.req.SCANsatCoverage.atLeast", minCoverage.ToString("N0"), SCANsatCoverage.ScanDisplayName(scanType), body);
            }
            else
            {
                return Localizer.Format("#cc.scansat.req.SCANsatCoverage.atMost", maxCoverage.ToString("N0"), SCANsatCoverage.ScanDisplayName(scanType), body);
            }
        }
    }
}
