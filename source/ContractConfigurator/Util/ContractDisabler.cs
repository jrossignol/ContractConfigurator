using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// Static class for disabling and tracking state of stock-style contracts.
    /// </summary>
    public static class ContractDisabler
    {
        private class ContractDetails
        {
            public Type contractType;
            public List<ContractGroup> disablingGroups = new List<ContractGroup>();

            public ContractDetails(Type contractType)
            {
                this.contractType = contractType;
            }
        }
        private static Dictionary<Type, ContractDetails> contractDetails = new Dictionary<Type, ContractDetails>();

        public static void SetContractToDisabled(Type contract, ContractGroup group)
        {
            if (!contractDetails.ContainsKey(contract))
            {
                contractDetails[contract] = new ContractDetails(contract);
            }
            ContractDetails details = contractDetails[contract];

            details.disablingGroups.AddUnique(group);
            SetContractState(contract, false);
        }

        public static void SetContractState(Type contract, bool enabled)
        {
            if (!enabled && ContractSystem.ContractTypes.Contains(contract))
            {
                LoggingUtil.LogDebug(typeof(ContractDisabler), "Disabling ContractType: " + contract.FullName + " (" + contract.Module + ")");
                ContractSystem.ContractTypes.Remove(contract);
            }
            else if (enabled && !ContractSystem.ContractTypes.Contains(contract))
            {
                LoggingUtil.LogDebug(typeof(ContractDisabler), "Enabling ContractType: " + contract.FullName + " (" + contract.Module + ")");
                ContractSystem.ContractTypes.Add(contract);
            }
        }

        public static bool IsEnabled(Type contract)
        {
            return ContractSystem.ContractTypes.Contains(contract);
        }

        public static IEnumerable<ContractGroup> DisablingGroups(Type contract)
        {
            if (!contractDetails.ContainsKey(contract))
            {
                return Enumerable.Empty<ContractGroup>();
            }

            return contractDetails[contract].disablingGroups;
        }
    }
}
