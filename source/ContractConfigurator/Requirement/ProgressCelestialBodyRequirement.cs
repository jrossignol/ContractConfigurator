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
        enum CheckType
        {
            UNMANNED,
            MANNED
        }
        CheckType? checkType;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ValidateTargetBody(configNode);
            valid &= ConfigNodeUtil.ParseValue<CheckType?>(configNode, "checkType", x => checkType = x, this, (CheckType?)null);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            if (checkType != null)
            {
                configNode.AddValue("checkType", checkType);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            checkType = ConfigNodeUtil.ParseValue<CheckType?>(configNode, "checkType", (CheckType?)null);
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
            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return false;
            }

            // Validate the CelestialBodySubtree exists
            CelestialBodySubtree cbProgress = GetCelestialBodySubtree();
            if (cbProgress == null)
            {
                LoggingUtil.LogError(this, ": ProgressNode for targetBody " + targetBody.bodyName + " not found.");
                return false;
            }

            if (checkType == CheckType.MANNED)
            {
                return cbProgress.IsReached && cbProgress.IsCompleteManned;
            }
            else if (checkType == CheckType.UNMANNED)
            {
                return cbProgress.IsReached && cbProgress.IsCompleteUnmanned;
            }

            return true;
        }
    }
}
