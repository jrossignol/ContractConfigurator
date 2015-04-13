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
    /// ContractRequirement to provide requirement for player having researched a technology.
    /// </summary>
    public class TechResearchedRequirement : ContractRequirement
    {
        protected List<string> techs;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Check on active contracts too
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : true;

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "tech", x => techs = x, this, new List<string>());

            if (configNode.HasValue("part"))
            {
                List<AvailablePart> parts = new List<AvailablePart>();
                valid &= ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part", x => parts = x, this);

                foreach (AvailablePart part in parts)
                {
                    techs.AddUnique(part.TechRequired);
                }
            }

            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "tech", "part" }, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            foreach (string tech in techs)
            {
                ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(tech);
                if (techNode == null || techNode.state != RDTech.State.Available)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
