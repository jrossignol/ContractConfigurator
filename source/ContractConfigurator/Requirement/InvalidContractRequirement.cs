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
        public override bool Load(ConfigNode configNode)
        {
            base.Load(configNode);
            return false;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            throw new InvalidOperationException("Cannot check invalid requirement.");
        }
    }
}
