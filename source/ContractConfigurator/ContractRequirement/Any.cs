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
            foreach (ContractRequirement requirement in childNodes)
            {
                bool nodeMet = requirement.RequirementMet(contract);
                LoggingUtil.LogVerbose(typeof(ContractRequirement), "Checked requirement '" + requirement.Name + "' of type " + requirement.Type + ": " + (requirement.InvertRequirement ? !nodeMet : nodeMet));
                requirementMet |= (requirement.InvertRequirement ? !nodeMet : nodeMet);
            }
            return requirementMet;
        }
    }
}
