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
     * ContractRequirement to provide requirement for player having a certain amount of science.
     */
    public class ScienceRequirement : ContractRequirement
    {
        protected float minScience;
        protected float maxScience;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minScience", x => minScience = x, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxScience", x => maxScience = x, this, float.MaxValue, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minScience", "maxScience" }, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            float science = ResearchAndDevelopment.Instance.Science;
            return science >= minScience && science <= maxScience;
        }
    }
}
