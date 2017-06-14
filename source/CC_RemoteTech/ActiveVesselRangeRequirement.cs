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
    /// ContractRequirement to check the range for an active vessel at the given celestial body.
    /// </summary>
    public class ActiveVesselRangeRequirement : ContractRequirement
    {
        protected double range;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Before loading, verify the RemoteTech version
            valid &= Util.Version.VerifyRemoteTechVersion();

            // Do not check on active contracts
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "range", x => range = x, this);
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("range", range);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            range = ConfigNodeUtil.ParseValue<double>(configNode, "range", 0.0);
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            LoggingUtil.LogVerbose(this, "Checking requirement");

            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return false;
            }

            return RemoteTechProgressTracker.Instance.ActiveRange(targetBody) > range;
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have a RemoteTech constellation orbiting " + (targetBody == null ? "the target body" : targetBody.CleanDisplayName(true)) + " with an antenna or dish aimed at the Active Vessel with a range of at least " + (range / 1000.0).ToString("N0") + " km";
            return output;
        }
    }
}
