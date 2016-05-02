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

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            Debug.Log("PartModuleUnlockedRequirement.LoadFromConfig");
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Do not check on active contracts.
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", x => partModules = x, this, x => x.All(Validation.ValidatePartModule));

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            Debug.Log("PartModuleUnlockedRequirement.OnSave");
            foreach (string pm in partModules)
            {
                configNode.AddValue("partModule", pm);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            Debug.Log("PartModuleUnlockedRequirement.OnLoad");
            partModules = ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModule", new List<string>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            Debug.Log("PartModuleUnlockedRequirement.RequirementMet");
            Debug.Log("    partModules = " + partModules);
            foreach (string partModule in partModules)
            {
                Debug.Log("    partModule = " + partModule);
                Debug.Log("    PartLoader.Instance = " + PartLoader.Instance);
                Debug.Log("    PartLoader.Instance.parts = " + PartLoader.Instance.parts);

                // Should never happen?
                if (PartLoader.Instance == null || PartLoader.Instance.parts == null)
                {
                    return false;
                }


                // Search for a part that has our module
                bool found = false;
                foreach (AvailablePart p in PartLoader.Instance.parts)
                {
                    Debug.Log("        p = " + p);
                    Debug.Log("        p.partPrefab = " + p.partPrefab);
                    Debug.Log("        p.partPrefab.Modules = " + p.partPrefab.Modules);
                    if (p != null && p.partPrefab != null && p.partPrefab.Modules != null)
                    {
                        foreach (PartModule pm in p.partPrefab.Modules)
                        {
                            Debug.Log("            pm = " + pm);
                            if (pm != null && pm.moduleName != null && pm.moduleName == partModule)
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
