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
    /*
     * ContractRequirement to check whether the player has a facility of a certain level.
     */
    public class FacilityRequirement : ContractRequirement
    {
        protected string facility { get; set; }
        protected int minLevel { get; set; }
        protected int maxLevel { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get trait
            if (!configNode.HasValue("facility"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'facility'.");
            }
            else
            {
                facility = configNode.GetValue("facility");
            }

            // Get minCount
            if (configNode.HasValue("minLevel"))
            {
                minLevel = Convert.ToInt32(configNode.GetValue("minLevel"));
            }
            else
            {
                minLevel = 1;
            }

            // Get maxCount
            if (configNode.HasValue("maxLevel"))
            {
                maxLevel = Convert.ToInt32(configNode.GetValue("maxLevel"));
            }
            else
            {
                maxLevel = int.MaxValue;
            }

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            UpgradeableFacility upgradeableFacility = UnityEngine.Object.FindObjectsOfType<SpaceCenterBuilding>().
                Where<SpaceCenterBuilding>(b => b.facilityName == facility).First<SpaceCenterBuilding>().Facility;

            int level = upgradeableFacility.FacilityLevel;
            return level >= minLevel && level <= maxLevel;
        }
    }
}
