using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Expansions;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for having certain expansion installed.
    /// </summary>
    public class ExpansionRequirement : ContractRequirement
    {
        protected enum Expansion
        {
            [Description("#autoLOC_8400166")]       MakingHistory,
            [Description("#cc.expansion.Serenity")] Serenity
        }
        protected Expansion expansion;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<Expansion>(configNode, "expansion", x => expansion = x, this);

            // Not invertable
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false, x => Validation.EQ(x, false));

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("expansion", expansion);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            expansion = ConfigNodeUtil.ParseValue<Expansion>(configNode, "expansion");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return ExpansionsLoader.IsExpansionInstalled(expansion.ToString());
        }

        protected override string RequirementText()
        {
            return Localizer.Format("#cc.req.Expansion", expansion.displayDescription());
        }
    }
}
