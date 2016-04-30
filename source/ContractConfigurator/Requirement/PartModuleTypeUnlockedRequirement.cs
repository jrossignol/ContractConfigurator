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

            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "partModuleType", x => partModuleType = x, this);

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
            // Late validation
            partModuleType.All(Validation.ValidatePartModuleType);

            // Actual check
            return partModuleType.All(s => ProgressUtilities.HaveModuleTypeTech(s));
        }
    }
}
