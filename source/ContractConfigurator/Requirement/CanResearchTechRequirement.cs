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

        private static ConfigNode techTree = null;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

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

        public override void OnSave(ConfigNode configNode)
        {
            foreach (string tech in techs)
            {
                configNode.AddValue("tech", tech);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            techs = ConfigNodeUtil.ParseValue<List<string>>(configNode, "tech", new List<string>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Cache the tech tree
            if (techTree == null)
            {
                ConfigNode techTreeRoot = ConfigNode.Load(HighLogic.CurrentGame.Parameters.Career.TechTreeUrl);
                if (techTreeRoot != null)
                {
                    techTree = techTreeRoot.GetNode("TechTree");
                }

                if (techTreeRoot == null || techTree == null)
                {
                    LoggingUtil.LogError(this, "Couldn't load tech tree from " + HighLogic.CurrentGame.Parameters.Career.TechTreeUrl);
                    return false;
                }
            }

            foreach (string tech in techs)
            {
                ConfigNode techNode = techTree.GetNodes("RDNode").Where(n => n.GetValue("id") == tech).FirstOrDefault();

                if (techNode == null)
                {
                    LoggingUtil.LogWarning(this, "No tech node found with id '" + tech + "'");
                    return false;
                }

                // Get the state of the parents, as well as the anyToUnlock flag
                bool anyToUnlock = ConfigNodeUtil.ParseValue<bool>(techNode, "anyToUnlock");
                IEnumerable<bool> parentsUnlocked = techNode.GetNodes("Parent").
                    Select(n => ResearchAndDevelopment.Instance.GetTechState(n.GetValue("parentID"))).
                    Select(p => p != null && p.state == RDTech.State.Available);

                // Check if the parents have met the unlock criteria
                if (anyToUnlock && parentsUnlocked.Any(unlocked => unlocked) ||
                    !anyToUnlock && parentsUnlocked.All(unlocked => unlocked))
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        protected override string RequirementText()
        {
            string techStr = "";
            for (int i = 0; i < techs.Count; i++)
            {
                if (i != 0)
                {
                    if (i == techs.Count - 1)
                    {
                        techStr += " and ";
                    }
                    else
                    {
                        techStr += ", ";
                    }
                }

                techStr += Tech.GetTech(techs[i]).title;
            }

            return "Must " + (invertRequirement ? "not " : "") + " be able to research (or have already researched) " + techStr;
        }
    }
}
