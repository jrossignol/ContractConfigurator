using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for disabling and tracking state of stock-style contracts.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ContractDisabler : MonoBehaviour
    {
        public static ContractDisabler Instance;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

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
        public static bool contractsDisabled = false;

        public static void SetContractToDisabled(string contract, ContractGroup group)
        {
            Type contractType = contractTypes.Where(t => t.Name == contract).FirstOrDefault();
            if (contractType == null)
            {
                LoggingUtil.LogWarning(typeof(ContractDisabler), "Couldn't find ContractType '{0}' to disable.", contract);
            }

            if (!contractDetails.ContainsKey(contractType))
            {
                contractDetails[contractType] = new ContractDetails(contractType);
            }
            ContractDetails details = contractDetails[contractType];

            details.disablingGroups.AddUnique(group);
            SetContractState(contractType, false);
        }

        public static void SetContractState(Type contractType, bool enabled)
        {
            if (ContractSystem.ContractTypes == null || ContractSystem.Instance == null)
            {
                Instance.StartCoroutine(Instance.SetContractStateDeferred(contractType, enabled));
            }
            else
            {
                if (!enabled && ContractSystem.ContractTypes.Contains(contractType))
                {
                    LoggingUtil.LogDebug(typeof(ContractDisabler), "Disabling ContractType: {0} ({1})", contractType.FullName, contractType.Module);
                    do
                    {
                        ContractSystem.ContractTypes.Remove(contractType);
                    } while (ContractSystem.ContractTypes.Contains(contractType));

                    // Remove Offered and active contracts 
                    foreach (Contract contract in ContractSystem.Instance.Contracts.Where(c => c != null && c.GetType() == contractType &&
                        (c.ContractState == Contract.State.Offered || c.ContractState == Contract.State.Active)))
                    {
                        contract.Withdraw();
                    }
                }
                else if (enabled && !ContractSystem.ContractTypes.Contains(contractType))
                {
                    LoggingUtil.LogDebug(typeof(ContractDisabler), "Enabling ContractType: {0} ({1})", contractType.FullName, contractType.Module);
                    ContractSystem.ContractTypes.Add(contractType);
                }
            }
        }

        public IEnumerator SetContractStateDeferred(Type contractType, bool enabled)
        {
            while (ContractSystem.ContractTypes == null || ContractSystem.Instance == null)
            {
                yield return null;
            }

            SetContractState(contractType, enabled);
        }


        public static bool IsEnabled(Type contract)
        {
            return ContractSystem.ContractTypes != null && ContractSystem.ContractTypes.Contains(contract);
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
