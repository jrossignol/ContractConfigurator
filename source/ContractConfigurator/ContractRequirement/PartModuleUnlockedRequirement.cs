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
    /// ContractRequirement to provide requirement for player having unlocked a part with a particular module.
    /// </summary>
    public class PartModuleUnlockedRequirement : ContractRequirement
    {
        protected List<string> partModules;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Do not check on active contracts.
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", x => partModules = x, this, x => x.All(Validation.ValidatePartModule));

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            foreach (string partModule in partModules)
            {
                // Search for a part that has our module
                bool found = false;
                foreach (AvailablePart p in PartLoader.Instance.parts)
                {
                    if (p.partPrefab != null && p.partPrefab.Modules != null)
                    {
                        foreach (PartModule pm in p.partPrefab.Modules)
                        {
                            if (pm.moduleName == partModule)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found)
                    {
                        if (ResearchAndDevelopment.PartTechAvailable(p))
                        {
                            break;
                        }
                        else
                        {
                            found = false;
                        }
                    }
                }

                if (!found)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
