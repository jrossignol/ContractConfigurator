using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using FinePrint;
using FinePrint.Utilities;
using KSP.Localization;
using ContractConfigurator.Util;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for a player unlocking a "type" of module.
    /// </summary>
    public class PartModuleTypeUnlockedRequirement : ContractRequirement
    {
        protected List<string> partModuleTypes;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Do not check on active contracts.
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", x => partModuleTypes = x, this, x => x.All(Validation.ValidatePartModuleType));

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            foreach (string pmt in partModuleTypes)
            {
                configNode.AddValue("partModuleType", pmt);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            partModuleTypes = ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", new List<string>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            foreach (string partModuleType in partModuleTypes)
            {
                foreach (AvailablePart part in PartLoader.LoadedPartsList)
                {
                    if (part.partPrefab == null || part.partPrefab.Modules == null)
                    {
                        continue;
                    }

                    if (part.partPrefab.HasValidContractObjective(partModuleType) && ResearchAndDevelopment.PartTechAvailable(part) && ResearchAndDevelopment.PartModelPurchased(part))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override string RequirementText()
        {
            return Localizer.Format(invertRequirement ? "#cc.req.PartModuleTypeUnlocked.x" : "#cc.req.PartModuleTypeUnlocked",
                LocalizationUtil.LocalizeList<string>(invertRequirement ? LocalizationUtil.Conjunction.AND : LocalizationUtil.Conjunction.OR, partModuleTypes,
                x => StringBuilderCache.Format("<color=#{0}" + ">{1}</color>", MissionControlUI.RequirementHighlightColor, Parameters.PartValidation.ModuleTypeName(x))),
                partModuleTypes.Count);
        }
    }
}
