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
        protected string tech { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get technology
            if (!configNode.HasValue("tech"))
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": missing required value 'tech'.");
            }
            else
            {
                tech = configNode.GetValue("tech");

                // Unfortunately, tech doesn't even seem to get loaded until it's available for
                // research, so it seems to be impossible to validate that we're referring to a
                // valid tech. :(
            }

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
