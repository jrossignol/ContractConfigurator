using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Upgradeables;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to check whether the player has a facility of a certain level.
    /// </summary>
    public class FacilityRequirement : ContractRequirement
    {
        protected string facility;
        protected int minLevel;
        protected int maxLevel;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Check on active contracts too
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : true;

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "facility", x => facility = x, this);
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "minLevel", x => minLevel = x, this, 1, x => Validation.Between(x, 0, 3));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxLevel", x => maxLevel = x, this, 3, x => Validation.Between(x, 0, 3));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minLevel", "maxLevel" }, this);

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            IEnumerable<UpgradeableFacility> facilities = UnityEngine.Object.FindObjectsOfType<UpgradeableFacility>().
                Where<UpgradeableFacility>(f => f.name == facility);

            if (facilities.Count() > 0)
            {
                UpgradeableFacility upgradeableFacility = facilities.First<UpgradeableFacility>();

                int level = upgradeableFacility.FacilityLevel;
                return level >= minLevel && level <= maxLevel;
            }
            else
            {
                // Should only get here if the facility name entered was bad
                LoggingUtil.LogError(this, "Coudn't find facility with name '" + facility + "'!");
                return false;
            }
        }
    }
}
