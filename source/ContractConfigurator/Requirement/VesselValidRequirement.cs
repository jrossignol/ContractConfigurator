using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to check if a VesselIdentifier is assigned to a valid vessel.
    /// </summary>
    public class ValidVesselRequirement : ContractRequirement
    {
        protected VesselIdentifier vessel;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Get expression
            valid &= ConfigNodeUtil.ParseValue<VesselIdentifier>(configNode, "vessel", x => vessel = x, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("vessel", vessel.identifier);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            vessel = ConfigNodeUtil.ParseValue<VesselIdentifier>(configNode, "vessel");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ContractVesselTracker.Instance != null && ContractVesselTracker.Instance.GetAssociatedVessel(vessel.identifier) != null;
        }
    }
}
