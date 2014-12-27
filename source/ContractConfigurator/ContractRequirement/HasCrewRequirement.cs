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
     * ContractRequirement that requires the player to have crew of a certain type and/or
     * experience level.
     */
    public class HasCrewRequirement : ContractRequirement
    {
        protected string trait;
        protected int minExperience;
        protected int maxExperience;
        protected int minCount;
        protected int maxCount;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "trait", ref trait, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minExperience", ref minExperience, this, 0, x => Validation.Between(x, 0, 5));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxExperience", ref maxExperience, this, 5, x => Validation.Between(x, 0, 5));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", ref minCount, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", ref maxCount, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minExperience", "maxExperience", "minCount", "maxCount" }, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            IEnumerable<ProtoCrewMember> crew = HighLogic.CurrentGame.CrewRoster.Crew;

            // Filter by trait
            if (trait != null)
            {
                crew = crew.Where<ProtoCrewMember>(cm => cm.experienceTrait.TypeName == trait);
            }

            // Filter by experience
            crew = crew.Where<ProtoCrewMember>(cm => cm.experienceLevel >= minExperience && cm.experienceLevel <= maxExperience);

            // Check counts
            int count = crew.Count();
            return count >= minCount && count <= maxCount;
        }
    }
}
