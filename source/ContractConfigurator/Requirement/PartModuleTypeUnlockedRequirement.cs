using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using FinePrint;
using FinePrint.Utilities;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for a player unlocking a "type" of module.
    /// </summary>
    public class PartModuleTypeUnlockedRequirement : ContractRequirement
    {
        protected List<string> partModuleType;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            // Do not check on active contracts.
            checkOnActiveContract = configNode.HasValue("checkOnActiveContract") ? checkOnActiveContract : false;

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", x => partModuleType = x, this, x => x.All(Validation.ValidatePartModuleType));

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            foreach (string pmt in partModuleType)
            {
                configNode.AddValue("partModuleType", pmt);
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            partModuleType = ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", new List<string>());
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return partModuleType.All(s => ProgressUtilities.HaveModuleTypeTech(s));
        }

        protected override string RequirementText()
        {
            string partStr = "";
            for (int i = 0; i < partModuleType.Count; i++)
            {
                if (i != 0)
                {
                    if (i == partModuleType.Count - 1)
                    {
                        partStr += " or ";
                    }
                    else
                    {
                        partStr += ", ";
                    }
                }

                partStr += partModuleType[i];
            }

            return "Must " + (invertRequirement ? "not " : "") + "have a part unlocked of type " + partStr;
        }
    }
}
