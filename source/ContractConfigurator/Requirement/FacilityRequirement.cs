using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Upgradeables;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to check whether the player has a facility of a certain level.
    /// </summary>
    public class FacilityRequirement : ContractRequirement
    {
        protected SpaceCenterFacility facility;
        protected int minLevel;
        protected int maxLevel;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Check on active contracts too
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : true;

            valid &= ConfigNodeUtil.ParseValue<SpaceCenterFacility>(configNode, "facility", x => facility = x, this);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minLevel", x => minLevel = x, this, 1, x => Validation.Between(x, 1, 3));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxLevel", x => maxLevel = x, this, 3, x => Validation.Between(x, 1, 3));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minLevel", "maxLevel" }, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("facility", facility);
            configNode.AddValue("minLevel", minLevel);
            configNode.AddValue("maxLevel", maxLevel);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            facility = ConfigNodeUtil.ParseValue<SpaceCenterFacility>(configNode, "facility");
            minLevel = ConfigNodeUtil.ParseValue<int>(configNode, "minLevel");
            maxLevel = ConfigNodeUtil.ParseValue<int>(configNode, "maxLevel");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Don't check active contracts in any scene but space center
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER && contract != null && contract.ContractState == Contracts.Contract.State.Active)
            {
                return true;
            }

            int level = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(facility) *
                ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility)) + 1;
            return level >= minLevel && level <= maxLevel;
        }

        protected override string RequirementText()
        {
            string facilityName = Regex.Replace(facility.ToString(), @"([A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1");
            if (facility == SpaceCenterFacility.Administration || facility == SpaceCenterFacility.MissionControl || facility == SpaceCenterFacility.ResearchAndDevelopment)
            {
                facilityName += " Building";
            }

            string output = "The " + facilityName + " must " + (invertRequirement ? "not " : "") + "be ";
            if (minLevel == maxLevel)
            {
                output += "at level " + NumericValueExpressionParser<int>.PrintNumber(minLevel);
            }
            else if (minLevel >= 1)
            {
                output += "at least at level " + NumericValueExpressionParser<int>.PrintNumber(minLevel);
            }
            else
            {
                output += "at most at level " + NumericValueExpressionParser<int>.PrintNumber(maxLevel);
            }

            return output;
        }
    }
}