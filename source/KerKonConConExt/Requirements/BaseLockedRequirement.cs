using System;
using KerbalKonstructs.LaunchSites;
using ContractConfigurator;
using ContractConfigurator.Util;

namespace KerKonConConExt
{
    public class BaseLockedRequirement : ContractRequirement
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
            return LaunchSiteManager.getIsSiteLocked(basename);
        }

        protected override string RequirementText()
        {
            string output = "Base <color=#" + MissionControlUI.RequirementHighlightColor + ">'" + basename + "'</color> must " + (invertRequirement ? "not be locked" : "be locked");
            return output;
        }
    }
}