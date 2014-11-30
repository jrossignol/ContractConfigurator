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
     * ContractRequirement set requirement.  Requirement is met if all child requirements are met.
     */
    public class AllRequirement : ContractRequirement
    {
        public override bool RequirementMet(ContractType contractType)
        {
            bool requirementMet = true;
            foreach (ContractRequirement contractRequirement in childNodes)
            {
                requirementMet &= contractRequirement.RequirementMet(contractType);
            }
            return requirementMet;
        }
    }
}
