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
    /// ContractRequirement to provide requirement for player being able to research a technology.
    /// </summary>
    public class CanResearchTechRequirement : ContractRequirement
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
                RDNode node = RDController.Instance.nodes.Where(rdn => rdn.tech.techID == tech).First();

                if (!node.IsResearched &&
                    !(node.AnyParentToUnlock && node.parents.Any(p => p.parent.node.IsResearched)) &&
                    !(!node.AnyParentToUnlock && node.parents.All(p => p.parent.node.IsResearched)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
