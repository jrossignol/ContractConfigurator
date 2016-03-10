using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide requirement for player having completed other contracts.
    /// </summary>
    public class CompleteContractRequirement : ContractCheckRequirement
    {
        protected Duration cooldownDuration;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "cooldownDuration", x => cooldownDuration = x, this, new Duration(0.0));

            return valid;
        }

        public override void SaveToPersistence(ConfigNode configNode)
        {
            base.SaveToPersistence(configNode);

            configNode.AddValue("cooldownDuration", cooldownDuration.Value);
        }

        public override void LoadFromPersistence(ConfigNode configNode)
        {
            base.LoadFromPersistence(configNode);

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
                IEnumerable<ConfiguredContract> completedContract = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().
                    Where(c => c.contractType != null && c.contractType.name.Equals(ccType));
                finished = completedContract.Count();
                if (finished > 0)
                {
                    lastFinished = completedContract.OrderByDescending<ConfiguredContract, double>(c => c.DateFinished).First().DateFinished;
                }
            }
            // Finished contracts - stock style
            else
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
    }
}
