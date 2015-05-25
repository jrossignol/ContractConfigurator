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
    public class CompleteContractRequirement : ContractRequirement
    {
        protected string ccType;
        protected Type contractClass;
        protected uint minCount;
        protected uint maxCount;
        protected Duration cooldownDuration;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            checkOnActiveContract = true;

            // Get type
            string contractType = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "contractType", x => contractType = x, this);
            if (valid)
            {
                if (ContractType.GetContractType(contractType) != null)
                {
                    ccType = contractType;
                }
                else
                {
                    ccType = null;

                    IEnumerable<Type> classes = ContractConfigurator.GetAllTypes<Contract>().Where(t => t.Name == contractType);
                    if (!classes.Any())
                    {
                        valid = false;
                        LoggingUtil.LogError(this.GetType(), "contractType '" + contractType +
                            "' must either be a Contract sub-class or ContractConfigurator contract type");
                    }
                    else
                    {
                        contractClass = classes.First();
                    }
                }
            }

            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "minCount", x => minCount = x, this, 1);
            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "maxCount", x => maxCount = x, this, UInt32.MaxValue);
            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "cooldownDuration", x => cooldownDuration = x, this, new Duration(0.0));

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Get the count of finished contracts
            int finished = 0;
            double lastFinished = 0.0;

            // Finished contracts - Contract Configurator style
            if (ccType != null)
            {
                IEnumerable<ConfiguredContract> completedContract = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().Where(c => c.contractType.name.Equals(ccType));
                finished = completedContract.Count();
                if (finished > 0)
                {
                    // TODO - this isn't working out
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
