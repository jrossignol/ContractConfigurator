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
    /// ContractRequirement set requirement.  Requirement is met if all child requirements are met.
    /// </summary>
    public class AllRequirement : ContractRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            bool requirementMet = true;
            foreach (ContractRequirement requirement in childNodes)
            {
                if (requirement.enabled)
                {
                    requirementMet &= requirement.CheckRequirement(contract);

                    if (!requirementMet)
                    {
                        return false;
                    }
                }
            }
            return requirementMet;
        }
    }
}
