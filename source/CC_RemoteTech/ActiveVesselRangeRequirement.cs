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

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Before loading, verify the RemoteTech version
            valid &= Util.VerifyRemoteTechVersion();

            // Do not check on active contracts
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "range", x => range = x, this);
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            LoggingUtil.LogVerbose(this, "Checking requirement");
            return RemoteTechProgressTracker.Instance.ActiveRange(targetBody) > range;
        }
    }
}
