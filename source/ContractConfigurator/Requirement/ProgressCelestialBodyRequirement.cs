using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using KSP.Localization;

namespace ContractConfigurator
{
    /// <summary>
    /// Base class for all ContractRequirement classes that use a celestial body.
    /// </summary>
    public abstract class ProgressCelestialBodyRequirement : ContractRequirement
    {
        protected string tag;
        protected string tagx;

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

            tag  = StringBuilderCache.Format("#cc.req.{0}", type);
            tagx = StringBuilderCache.Format("#cc.req.{0}.x", type);

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

        protected ProgressNode GetCelestialBodySubtree()
        {
            // Get the progress tree for our celestial body
            foreach (var node in ProgressTracking.Instance.celestialBodyNodes)
            {
                if (node.Body == targetBody)
                {
                    return GetTypeSpecificProgressNode(node);
                }
            }

            return null;
        }

        protected abstract ProgressNode GetTypeSpecificProgressNode(CelestialBodySubtree celestialBodySubtree);

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return false;
            }

            // Validate the CelestialBodySubtree exists
            ProgressNode cbProgress = GetCelestialBodySubtree();
            if (cbProgress == null)
            {
                LoggingUtil.LogError(this, "{0}: ProgressNode for targetBody {1} not found.", (contract != null ? contract.contractType.name : "Unknown contract"), targetBody.bodyName);
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

        protected int CheckTypeId()
        {
            return checkType == null ? 0 : checkType == CheckType.MANNED ? 2 : 1;
        }

        protected override string RequirementText()
        {
            return Localizer.Format(invertRequirement ? tagx : tag, CheckTypeId(),
                targetBody == null ? Localizer.GetStringByTag("#cc.req.ProgressCelestialBody.genericBody") : targetBody.CleanDisplayName(true));
        }
    }
}
