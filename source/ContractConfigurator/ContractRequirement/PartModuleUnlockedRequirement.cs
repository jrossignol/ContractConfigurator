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
     * ContractRequirement to provide requirement for player having unlocked a part with a particular module.
     */
    public class PartModuleUnlockedRequirement : ContractRequirement
    {
        protected string partModule { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get partModule
            if (!configNode.HasValue("partModule"))
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": missing required value 'partModule'.");
            }
            else
            {
                partModule = configNode.GetValue("partModule");
                valid &= ConfigNodeUtil.ValidatePartModule(partModule);
            }

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Search for a part that has our module
            foreach (AvailablePart p in PartLoader.Instance.parts)
            {
                if (p.partPrefab != null && p.partPrefab.Modules != null)
                {
                    foreach (PartModule pm in p.partPrefab.Modules)
                    {
                        if (pm.moduleName == partModule)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
