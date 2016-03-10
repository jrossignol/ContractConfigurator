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

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Do not check on active contracts.
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => parts = x, this);

            return valid;
        }

        public override void SaveToPersistence(ConfigNode configNode)
        {
            base.SaveToPersistence(configNode);

            foreach (AvailablePart part in parts)
            {
                configNode.AddValue("part", part.name);
            }
        }

        public override void LoadFromPersistence(ConfigNode configNode)
        {
            base.LoadFromPersistence(configNode);

            parts = ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", new List<AvailablePart>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            foreach (AvailablePart part in parts)
            {
                if (!ResearchAndDevelopment.PartTechAvailable(part))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
