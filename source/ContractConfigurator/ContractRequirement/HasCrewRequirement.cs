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
        protected string trait { get; set; }
        protected int minExperience { get; set; }
        protected int maxExperience { get; set; }
        protected int minCount { get; set; }
        protected int maxCount { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get trait
            if (!configNode.HasValue("trait"))
            {
                trait = null;
            }
            else
            {
                try
                {
                    trait = configNode.GetValue("trait");
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                        ": error parsing trait: " + e.Message);
                }
            }

            // Get minimum experience level
            if (configNode.HasValue("minExperience"))
            {
                minExperience = Convert.ToInt32(configNode.GetValue("minExperience"));
            }
            else
            {
                minExperience = 0;
            }

            // Get maximum experience level
            if (configNode.HasValue("maxExperience"))
            {
                maxExperience = Convert.ToInt32(configNode.GetValue("maxExperience"));
            }
            else
            {
                maxExperience = 5;
            }

            // Get minCount
            if (configNode.HasValue("minCount"))
            {
                minCount = Convert.ToInt32(configNode.GetValue("minCount"));
            }
            else
            {
                minCount = 1;
            }

            // Get maxCount
            if (configNode.HasValue("maxCount"))
            {
                maxCount = Convert.ToInt32(configNode.GetValue("maxCount"));
            }
            else
            {
                maxCount = int.MaxValue;
            }

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
