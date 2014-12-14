using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /*
     * ContractRequirement set requirement.  Requirement is met if any child requirement is met.
     */
    public class AnyRequirement : ContractRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            bool requirementMet = false;
            foreach (ContractRequirement contractRequirement in childNodes)
            {
                requirementMet |= contractRequirement.RequirementMet(contract);
            }
            return requirementMet;
        }
    }
}
