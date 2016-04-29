using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /// <summary>
    /// Special placeholder for requirements that failed to load.
    /// </summary>
    public class InvalidContractRequirement : ContractRequirement
    {
        public override bool LoadFromConfig(ConfigNode configNode)
        {
            base.LoadFromConfig(configNode);
            return false;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            throw new InvalidOperationException("Cannot check invalid requirement.");
        }

        public override void OnLoad(ConfigNode configNode) { }
        public override void OnSave(ConfigNode configNode) { }
    }
}
