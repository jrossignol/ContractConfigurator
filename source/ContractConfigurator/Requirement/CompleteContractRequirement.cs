using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Util;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having completed other contracts.
    /// </summary>
    public class CompleteContractRequirement : ContractCheckRequirement
    {
        protected Duration cooldownDuration;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "cooldownDuration", x => cooldownDuration = x, this, new Duration(0.0));

            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);

            configNode.AddValue("cooldownDuration", cooldownDuration.Value);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            cooldownDuration = new Duration(ConfigNodeUtil.ParseValue<double>(configNode, "cooldownDuration"));
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Get the count of finished contracts
            int finished = 0;
            double lastFinished = 0.0;

            // Finished contracts - Contract Configurator style
            if (ccType != null)
            {
                IEnumerable<ConfiguredContract> completedContract = ConfiguredContract.CompletedContracts.
                    Where(c => c.contractType != null && c.contractType.name.Equals(ccType));
                finished = completedContract.Count();
                if (finished > 0)
                {
                    lastFinished = completedContract.OrderByDescending<ConfiguredContract, double>(c => c.DateFinished).First().DateFinished;
                }
            }
            // Finished contracts - stock style
            else if (contractClass != null)
            {
                // Call the GetCompletedContracts with our type, and get the count
                Contract[] completedContract = (Contract[])typeof(ContractSystem).GetMethod("GetCompletedContracts").MakeGenericMethod(contractClass).Invoke(ContractSystem.Instance, null);
                finished = completedContract.Count();
                if (finished > 0)
                {
                    lastFinished = completedContract.OrderByDescending<Contract, double>(c => c.DateFinished).First().DateFinished;
                }
            }

            // Check cooldown
            if (cooldownDuration.Value > 0.0 && finished > 0 && lastFinished + cooldownDuration.Value > Planetarium.GetUniversalTime())
            {
                LoggingUtil.LogDebug(this, "Returning false due to cooldown for " + contractType.name);
                return false;
            }

            // Return based on the min/max counts configured
            return (finished >= minCount) && (finished <= maxCount);
        }

        protected override string RequirementText()
        {
            string output = "Must " + (invertRequirement ? "not " : "") + "have completed contract <color=#" + MissionControlUI.RequirementHighlightColor + ">'" + ContractTitle() + "'</color>";
            if (cooldownDuration.Value > 0.0)
            {
                output += " within the last " + cooldownDuration.ToString();
            }

            return output;
        }
    }
}
