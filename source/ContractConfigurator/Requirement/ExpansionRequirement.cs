using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Expansions;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for having certain expansion installed.
    /// </summary>
    public class ExpansionRequirement : ContractRequirement
    {
        protected List<string> expansions;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "expansion", x => expansions = x, this, new List<string>());

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            foreach (string expansion in expansions)
            {
                configNode.AddValue("expansion", expansion);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            expansions = ConfigNodeUtil.ParseValue<List<string>>(configNode, "expansion", new List<string>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            foreach (string expansion in expansions)
            {
                if (!ExpansionsLoader.IsExpansionInstalled(expansion))
                {
                    return false;
                }
            }

            return true;
        }

        protected override string RequirementText()
        {
            string expansionStr = "";
            for (int i = 0; i < expansions.Count; i++)
            {
                if (i != 0)
                {
                    if (i == expansions.Count - 1)
                    {
                        expansionStr += " and ";
                    }
                    else
                    {
                        expansionStr += ", ";
                    }
                }

                expansionStr += Regex.Replace(expansions[i], @"([A-Z&]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
            }

            if (expansions.Count > 1)
            {
                expansionStr += " expansions";
            }
            else
            {
                expansionStr += " expanion";
            }

            return "Must " + (invertRequirement ? "not " : "") + " have the " + expansionStr + " installed.";
        }
    }
}
