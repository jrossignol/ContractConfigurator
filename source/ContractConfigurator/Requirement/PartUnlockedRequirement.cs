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
    /// ContractRequirement to provide requirement for player having unlocked a particular part.
    /// </summary>
    public class PartUnlockedRequirement : ContractRequirement
    {
        protected List<AvailablePart> parts;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Do not check on active contracts.
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => parts = x, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            foreach (AvailablePart part in parts)
            {
                configNode.AddValue("part", part.name);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            parts = ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", new List<AvailablePart>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            foreach (AvailablePart part in parts)
            {
                if (!ResearchAndDevelopment.PartModelPurchased(part))
                {
                    return false;
                }
            }
            return true;
        }

        protected override string RequirementText()
        {
            string partStr = "";
            for (int i = 0; i < parts.Count; i++)
            {
                if (i != 0)
                {
                    if (i == parts.Count - 1)
                    {
                        partStr += " and ";
                    }
                    else
                    {
                        partStr += ", ";
                    }
                }

                partStr += parts[i].title;
            }

            return "Must " + (invertRequirement ? "not " : "") + "have unlocked the " + partStr;
        }
    }
}
