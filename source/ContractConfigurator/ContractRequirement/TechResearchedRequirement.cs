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
     * ContractRequirement to provide requirement for player having researched a technology.
     */
    public class TechResearchedRequirement : ContractRequirement
    {
        protected string tech;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Check on active contracts too
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : true;

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "tech", ref tech, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(tech);
            if (techNode == null)
            {
                return false;
            }
            return techNode.state == RDTech.State.Available;
        }
    }
}
