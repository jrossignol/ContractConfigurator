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
    /// ContractRequirement to provide requirement for player having accepted other contracts.
    /// </summary>
    public class AcceptContractRequirement : ContractRequirement
    {
        protected string ccType;
        protected Type contractClass;
        protected uint minCount;
        protected uint maxCount;

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

            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            // Get the count of accepted contracts
            int accepted = 0;

            // Finished contracts - Contract Configurator style
            if (ccType != null)
            {
                IEnumerable<ConfiguredContract> acceptedContract = ContractSystem.Instance.Contracts.Select(c => c as ConfiguredContract).
                    Where(c => c != null && c.contractType != null && c.contractType.name.Equals(ccType) && c.ContractState == Contract.State.Active);
                accepted = acceptedContract.Count();
            }
            // Finished contracts - stock style
            else
            {
                // Call the GetCompletedContracts with our type, and get the count
                IEnumerable<Contract> acceptedContract = ContractSystem.Instance.Contracts.Where(c => c != null && c.GetType() == contractClass &&
                    c.ContractState == Contract.State.Active);
                accepted = acceptedContract.Count();
            }

            // Return based on the min/max counts configured
            return (accepted >= minCount) && (accepted <= maxCount);
        }
    }
}
