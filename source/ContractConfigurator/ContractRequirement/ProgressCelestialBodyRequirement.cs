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
    /// Base class for all ContractRequirement classes that use a celestial body.
    /// </summary>
    public abstract class ProgressCelestialBodyRequirement : ContractRequirement
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        protected CelestialBodySubtree GetCelestialBodySubtree()
        {
            // Get the progress tree for our celestial body
            foreach (var node in ProgressTracking.Instance.celestialBodyNodes)
            {
                if (node.Body == targetBody)
                {
                    return node;
                }
            }

            return null;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Target body could be null if an expression didn't satisfy its conditions
            if (targetBody == null)
            {
                return false;
            }

            // Validate the CelestialBodySubtree exists
            CelestialBodySubtree cbProgress = GetCelestialBodySubtree();
            if (cbProgress == null)
            {
                LoggingUtil.LogError(this.GetType(), ": ProgressNode for targetBody " + targetBody.bodyName + " not found.");
                return false;
            }
            return true;
        }
    }
}
