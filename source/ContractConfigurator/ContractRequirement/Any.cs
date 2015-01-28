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
    /// ContractRequirement set requirement.  Requirement is met if any child requirement is met.
    /// </summary>
    public class AnyRequirement : ContractRequirement
    {
        public override bool RequirementMet(ConfiguredContract contract)
        {
            bool requirementMet = false;
            foreach (ContractRequirement requirement in childNodes)
            {
                if (requirement.enabled)
                {
                    bool nodeMet = requirement.RequirementMet(contract);
                    requirement.lastResult = requirement.invertRequirement ? !nodeMet : nodeMet;
                    LoggingUtil.LogVerbose(typeof(ContractRequirement), "Checked requirement '" + requirement.Name + "' of type " + requirement.Type + ": " + (requirement.InvertRequirement ? !nodeMet : nodeMet));
                    requirementMet |= (requirement.InvertRequirement ? !nodeMet : nodeMet);

                    if (requirementMet)
                    {
                        return true;
                    }
                }
            }
            return requirementMet;
        }
    }
}
