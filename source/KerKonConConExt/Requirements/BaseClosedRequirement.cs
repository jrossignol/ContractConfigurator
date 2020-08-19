using System;
using KSP.Localization;
using KerbalKonstructs.LaunchSites;
using ContractConfigurator;
using ContractConfigurator.Util;

namespace KerKonConConExt
{
    public class BaseClosedRequirement : ContractRequirement
    {
        protected string basename { get; set; }

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "basename", x => basename = x, this);

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("basename", basename);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            basename = ConfigNodeUtil.ParseValue<String>(configNode, "basename");
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return !LaunchSiteManager.getIsSiteOpen(basename);
        }

        protected override string RequirementText()
        {
            return Localizer.Format(invertRequirement ? "#cc.kk.req.BaseOpen" : "#cc.kk.req.BaseClosed",
                StringBuilderCache.Format("<color=#{0}>{1}</color>", MissionControlUI.RequirementHighlightColor, basename));
        }
    }
}