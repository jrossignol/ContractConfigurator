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
        private static IEnumerable<Type> contractTypes = ContractConfigurator.GetAllTypes<Contract>();
        private static bool contractsDisabled = false;

        public static bool SetContractToDisabled(string contract, ContractGroup group)
        {
            Type contractType = contractTypes.Where(t => t.Name == contract).FirstOrDefault();
            if (contractType == null)
            {
                LoggingUtil.LogWarning(typeof(ContractDisabler), "Couldn't find ContractType '" + contract + "' to disable.");
                return false;
            }

            if (!contractDetails.ContainsKey(contractType))
            {
                contractDetails[contractType] = new ContractDetails(contractType);
            }
            ContractDetails details = contractDetails[contractType];

            details.disablingGroups.AddUnique(group);
            return SetContractState(contractType, false);
        }

        public static bool  SetContractState(Type contractType, bool enabled)
        {
            if (!enabled && ContractSystem.ContractTypes.Contains(contractType))
            {
                LoggingUtil.LogDebug(typeof(ContractDisabler), "Disabling ContractType: " + contractType.FullName + " (" + contractType.Module + ")");
                ContractSystem.ContractTypes.Remove(contractType);

                // Remove Offered and active contracts 
                foreach (Contract contract in ContractSystem.Instance.Contracts.Where(c => c != null && c.GetType() == contractType &&
                    (c.ContractState == Contract.State.Offered || c.ContractState == Contract.State.Active)))
                {
                    contract.Withdraw();
                }

                return true;
            }
            else if (enabled && !ContractSystem.ContractTypes.Contains(contractType))
            {
                LoggingUtil.LogDebug(typeof(ContractDisabler), "Enabling ContractType: " + contractType.FullName + " (" + contractType.Module + ")");
                ContractSystem.ContractTypes.Add(contractType);
                return true;
            }

            return false;
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

        /// <summary>
        /// Disables standard contract types as requested by contract packs.
        /// </summary>
        /// <returns>True if the disabling is done.</returns>
        public static bool DisableContracts()
        {
            if (contractsDisabled)
            {
                return true;
            }

            // Don't do anything if the contract system has not yet loaded
            if (ContractSystem.ContractTypes == null)
            {
                return false;
            }

            LoggingUtil.LogDebug(typeof(ContractDisabler), "Loading CONTRACT_CONFIGURATOR nodes.");
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("CONTRACT_CONFIGURATOR");

            int disabledCounter = 0;

            // Start disabling via legacy method
            Dictionary<string, Type> contractsToDisable = new Dictionary<string, Type>();
            foreach (ConfigNode node in nodes)
            {
                foreach (string contractType in node.GetValues("disabledContractType"))
                {
                    LoggingUtil.LogWarning(typeof(ContractDisabler), "Disabling contract " + contractType +
                        " via legacy method.  Recommend using the disableContractType attribute of the CONTRACT_GROUP node instead.");

                    if (SetContractToDisabled(contractType, null))
                    {
                        disabledCounter++;
                    }
                }
            }

            // Disable via new method
            foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g != null && g.parent == null))
            {
                foreach (string contractType in contractGroup.disabledContractType)
                {
                    if (SetContractToDisabled(contractType, contractGroup))
                    {
                        disabledCounter++;
                    }
                }
            }

            LoggingUtil.LogInfo(typeof(ContractDisabler), "Disabled " + disabledCounter + " ContractTypes.");

            contractsDisabled = true;
            return true;
        }
    }
}
