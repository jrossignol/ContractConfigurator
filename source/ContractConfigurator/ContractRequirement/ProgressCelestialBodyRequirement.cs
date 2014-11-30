using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;

namespace ContractConfigurator
{
    /*
     * Base class for all ContractRequirement classes that use a celestial body.
     */
    public abstract class ProgressCelestialBodyRequirement : ContractRequirement
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Load target body
            CelestialBody body = ConfigNodeUtil.ParseCelestialBody(configNode, "targetBody");
            if (body != null)
            {
                targetBody = body;
            }
            if (targetBody == null)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody for " + this.GetType().Name + " must be specified.");
            }

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

        public override bool RequirementMet(ContractType contractType)
        {
            // Validate the CelestialBodySubtree exists
            CelestialBodySubtree cbProgress = GetCelestialBodySubtree();
            if (cbProgress == null)
            {
                Debug.LogError("ContractConfigurator: " + this.GetType().Name + ": " +
                    ": ProgressNode for targetBody " + targetBody.bodyName + " not found.");
                return false;
            }
            return true;
        }
    }
}
