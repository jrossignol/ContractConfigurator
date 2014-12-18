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
        protected int scanType { get; set; }
        protected string scanTypeName { get; set; }
        protected double minCoverage { get; set; }
        protected double maxCoverage { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Do not check the requirement on active contracts.  Otherwise when they scan the
            // contract is invalidated, which is usually not what's meant.
            checkOnActiveContract = false;

            if (!configNode.HasValue("minCoverage"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'minCoverage'.");
            }
            else
            {
                try
                {
                    minCoverage = Convert.ToDouble(configNode.GetValue("minCoverage"));
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": can't parse value 'minCoverage': " + e.Message);
                }
            }

            if (!configNode.HasValue("maxCoverage"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'maxCoverage'.");
            }
            else
            {
                try
                {
                    maxCoverage = Convert.ToDouble(configNode.GetValue("maxCoverage"));
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": can't parse value 'maxCoverage': " + e.Message);
                }
            }

            if (!configNode.HasValue("scanType"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'scanType'.");
            }
            else
            {
                try
                {
                    scanTypeName = configNode.GetValue("scanType");
                    SCANdata.SCANtype scanTypeEnum = (SCANdata.SCANtype)Enum.Parse(typeof(SCANdata.SCANtype), scanTypeName);
                    scanType = (int)scanTypeEnum;
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": can't parse value 'scanType':" + e.Message);
                }
            }

            // Validate target body
            if (targetBody == null)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody for SCANsatCoverage must be specified.");
            }

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            double coverageInPercentage = SCANUtil.GetCoverage(scanType, targetBody);
            return coverageInPercentage >= minCoverage && coverageInPercentage <= maxCoverage;
        }
    }
}
