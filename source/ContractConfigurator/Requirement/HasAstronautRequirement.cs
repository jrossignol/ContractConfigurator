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
    /// ContractRequirement that requires the player to have crew of a certain type and/or
    /// experience level in their space program.
    /// </summary>
    public class HasAstronautRequirement : ContractRequirement
    {
        protected string trait;
        protected int minExperience;
        protected int maxExperience;
        protected int minCount;
        protected int maxCount;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "trait", x => trait = x, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minExperience", x => minExperience = x, this, 0, x => Validation.Between(x, 0, 5));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxExperience", x => maxExperience = x, this, 5, x => Validation.Between(x, 0, 5));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minCount", x => minCount = x, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCount", x => maxCount = x, this, int.MaxValue, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "trait", "minExperience", "maxExperience", "minCount", "maxCount" }, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            if (!string.IsNullOrEmpty(trait))
            {
                configNode.AddValue("trait", trait);
            }
            configNode.AddValue("minExperience", minExperience);
            configNode.AddValue("maxExperience", maxExperience);
            configNode.AddValue("minCount", minCount);
            configNode.AddValue("maxCount", maxCount);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            trait = ConfigNodeUtil.ParseValue<string>(configNode, "trait", (string)null);
            minExperience = ConfigNodeUtil.ParseValue<int>(configNode, "minExperience");
            maxExperience = ConfigNodeUtil.ParseValue<int>(configNode, "maxExperience");
            minCount = ConfigNodeUtil.ParseValue<int>(configNode, "minCount");
            maxCount = ConfigNodeUtil.ParseValue<int>(configNode, "maxCount");
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

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have ";

            if (minCount == maxCount)
            {
                if (minCount == 1)
                {
                    output += "an astronaut";
                }
                else
                {
                    output += "exactly " + minCount + " astronauts";
                }
            }
            else if (minCount > 0 && maxCount < int.MaxValue)
            {
                output += "between " + minCount + " and " + maxCount + " astronauts";
            }
            else if (minCount == 1)
            {
                output += "at least one astronaut";
            }
            else if (minCount > 0)
            {
                output += "at least " + minCount + " astronauts";
            }
            else if (maxCount < int.MaxValue)
            {
                output += "no more than " + maxCount + " astronauts";
            }

            if (minExperience > 0 && maxExperience < 5)
            {
                output += " with between " + minExperience + " and " + maxExperience + " experience levels";
            }
            else if (minExperience > 0)
            {
                output += " with at least " + minExperience + " experience levels";
            }
            else if (maxExperience < 5)
            {
                output += " with no more than  " + maxExperience + " experience levels";
            }

            return output;
        }
    }
}
