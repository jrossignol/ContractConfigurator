using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using KSP.Localization;
using ContractConfigurator.Util;

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
            return Localizer.Format(invertRequirement ? "#cc.req.PartUnlocked.x" : "#cc.req.PartUnlocked",
                LocalizationUtil.LocalizeList<AvailablePart>(invertRequirement ? LocalizationUtil.Conjunction.OR : LocalizationUtil.Conjunction.AND, parts,
                x => StringBuilderCache.Format("<color=#{0}>{1}</color>", MissionControlUI.RequirementHighlightColor, x.title)),
                parts.Count);
        }
    }
}
