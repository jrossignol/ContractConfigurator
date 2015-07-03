using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    public class ContractPreLoader : ScenarioModule
    {
        private class ContractDetails
        {
            public float lastGenerationFailure;
            public Queue<ConfiguredContract> contracts = new Queue<ConfiguredContract>();
            public Contract.ContractPrestige prestige;

            public ContractDetails(Contract.ContractPrestige prestige)
            {
                this.prestige = prestige;
            }
        }

        public static ContractPreLoader Instance { get; private set; }

        private const int MAX_CONTRACTS = 5;
        private const float MAX_TIME = 0.01f;
        private const float FAILURE_WAIT_TIME = 30.0f;

        private static int nextContractGroup = 0;
        System.Random rand = new System.Random();

        private Dictionary<Contract.ContractPrestige, ContractDetails> contractDetails = new Dictionary<Contract.ContractPrestige, ContractDetails>();
        private IEnumerator<KeyValuePair<ConfiguredContract, bool>?> currentEnumerator = null;
        private ContractDetails currentDetails = null;

        private string lastKey = null;

        public ContractPreLoader()
        {
            Instance = this;

            contractDetails[Contract.ContractPrestige.Trivial] = new ContractDetails(Contract.ContractPrestige.Trivial);
            contractDetails[Contract.ContractPrestige.Significant] = new ContractDetails(Contract.ContractPrestige.Significant);
            contractDetails[Contract.ContractPrestige.Exceptional] = new ContractDetails(Contract.ContractPrestige.Exceptional);
        }

        void Start()
        {
            GameEvents.Contract.onFinished.Add(new EventData<Contract>.OnEvent(OnContractFinish));
            GameEvents.Contract.onDeclined.Add(new EventData<Contract>.OnEvent(OnContractFinish));
            GameEvents.OnProgressReached.Add(new EventData<ProgressNode>.OnEvent(OnProgressReached));
        }

        void OnDestroy()
        {
            GameEvents.Contract.onFinished.Remove(new EventData<Contract>.OnEvent(OnContractFinish));
            GameEvents.Contract.onDeclined.Remove(new EventData<Contract>.OnEvent(OnContractFinish));
            GameEvents.OnProgressReached.Remove(new EventData<ProgressNode>.OnEvent(OnProgressReached));
        }

        void OnProgressReached(ProgressNode p)
        {
            // Reset the generation failures
            ResetGenerationFailure();
        }

        void OnContractFinish(Contract c)
        {
            // Reset the generation failure
            if (c != null)
            {
                ResetGenerationFailure(c.Prestige);
            }
        }

        void ResetGenerationFailure()
        {
            foreach (Contract.ContractPrestige prestige in contractDetails.Keys)
            {
                ResetGenerationFailure(prestige);
            }
        }

        void ResetGenerationFailure(Contract.ContractPrestige prestige)
        {
            LoggingUtil.LogVerbose(this, "Resetting generation failure marker for " + prestige);
            contractDetails[prestige].lastGenerationFailure = 0.0f;
        }

        void Update()
        {
            // Check if we need to make a new enumerator
            if (currentEnumerator == null)
            {
                // Prepare a list of possible selections
                IEnumerable<ContractDetails> selections = contractDetails.Values.Where(cd =>
                    UnityEngine.Time.realtimeSinceStartup - cd.lastGenerationFailure > FAILURE_WAIT_TIME &&
                    cd.contracts.Count < MAX_CONTRACTS);

                // Nothing is ready
                if (!selections.Any())
                {
                    return;
                }

                // Get a selection
                int r = rand.Next(selections.Count());
                currentDetails = selections.ElementAt(r);
                currentEnumerator = ContractGenerator(currentDetails.prestige).GetEnumerator();

                LoggingUtil.LogVerbose(this, "Got an enumerator, last failure time was " +
                    (UnityEngine.Time.realtimeSinceStartup - currentDetails.lastGenerationFailure) + " seconds ago");
                
            }

            // Loop through the enumerator until we run out of time, hit the end or generate a contract
            float start = UnityEngine.Time.realtimeSinceStartup;
            int count = 0;
            while (UnityEngine.Time.realtimeSinceStartup - start < MAX_TIME)
            {
                count++;
                if (!currentEnumerator.MoveNext())
                {
                    // We ran through the entire enumerator, mark the failure
                    LoggingUtil.LogVerbose(this, "Contract generation failure");
                    currentDetails.lastGenerationFailure = UnityEngine.Time.realtimeSinceStartup;
                    currentEnumerator = null;
                    break;
                }

                KeyValuePair<ConfiguredContract, bool>? pair = currentEnumerator.Current;
                if (pair != null && pair.Value.Value)
                {
                    currentDetails.contracts.Enqueue(pair.Value.Key);
                    currentEnumerator = null;
                    break;
                }
            }

            if (UnityEngine.Time.realtimeSinceStartup - start > 0.1)
            {
                LoggingUtil.LogDebug(this, "Contract attribute took too long (" + (UnityEngine.Time.realtimeSinceStartup - start) +
                    " seconds) to generate: " + lastKey);
            }
        }

        private IEnumerable<KeyValuePair<ConfiguredContract, bool>?> ContractGenerator(Contract.ContractPrestige prestige)
        {
            // Loop through all the contract groups
            IEnumerable<ContractGroup> groups = ContractGroup.AllGroups;
            foreach (ContractGroup group in groups.Skip(nextContractGroup).Concat(groups.Take(nextContractGroup)))
            {
                nextContractGroup = (nextContractGroup + 1) % groups.Count();

                foreach (KeyValuePair<ConfiguredContract, bool>? pair in ContractGenerator(prestige, group))
                {
                    yield return pair;
                }
            }
        }

        private IEnumerable<KeyValuePair<ConfiguredContract, bool>?> ContractGenerator(Contract.ContractPrestige prestige, ContractGroup group)
        {
            ConfiguredContract templateContract =
                Contract.Generate(typeof(ConfiguredContract), prestige, rand.Next(), Contract.State.Withdrawn) as ConfiguredContract;

            // Build a weighted list of ContractTypes to choose from
            Dictionary<ContractType, double> validContractTypes = new Dictionary<ContractType, double>();
            double totalWeight = 0.0;
            foreach (ContractType ct in ContractType.AllValidContractTypes.Where(ct => ct.group == group))
            {
                // Only select contracts with the correct prestige level
                if (ct.prestige.Count == 0 || ct.prestige.Contains(prestige))
                {
                    validContractTypes.Add(ct, ct.weight);
                    totalWeight += ct.weight;
                }
            }

            // Loop until we either run out of contracts in our list or make a selection
            while (validContractTypes.Count > 0)
            {
                ContractType selectedContractType = null;
                // Pick one of the contract types based on their weight
                double value = rand.NextDouble() * totalWeight;
                foreach (KeyValuePair<ContractType, double> pair in validContractTypes)
                {
                    value -= pair.Value;
                    if (value <= 0.0)
                    {
                        selectedContractType = pair.Key;
                        break;
                    }
                }

                // Shouldn't happen, but floating point rounding could put us here
                if (selectedContractType == null)
                {
                    selectedContractType = validContractTypes.First().Key;
                }

                // First, check the basic requirements
                if (!selectedContractType.MeetBasicRequirements(templateContract))
                {
                    LoggingUtil.LogVerbose(this, selectedContractType.name + " was not generated: basic requirements not met.");
                    validContractTypes.Remove(selectedContractType);
                    totalWeight -= selectedContractType.weight;

                    yield return null;
                    continue;
                }

                // Try to refresh non-deterministic values before we check extended requirements
                LoggingUtil.LogLevel origLogLevel = LoggingUtil.logLevel;
                LoggingUtil.LogLevel newLogLevel = selectedContractType.trace ? LoggingUtil.LogLevel.VERBOSE : LoggingUtil.logLevel;
                try
                {
                    // Set up for loop
                    LoggingUtil.logLevel = newLogLevel;
                    ConfiguredContract.currentContract = templateContract;

                    LoggingUtil.LogVerbose(this, "Refresh non-deterministic values for CONTRACT_TYPE = " + selectedContractType.name);
                    KeyValuePair<ConfiguredContract, bool> pair = new KeyValuePair<ConfiguredContract, bool>(templateContract, false);
                    foreach (string val in ConfigNodeUtil.UpdateNonDeterministicValuesIterator(selectedContractType.dataNode))
                    {
                        lastKey = selectedContractType.name + "[" + val + "]";

                        // Clear temp stuff
                        LoggingUtil.logLevel = origLogLevel;
                        ConfiguredContract.currentContract = null;

                        if (val == null)
                        {
                            LoggingUtil.LogVerbose(this, selectedContractType.name + " was not generated: non-deterministic expression failure.");
                            validContractTypes.Remove(selectedContractType);
                            totalWeight -= selectedContractType.weight;

                            yield return null;
                            continue;
                        }
                        else
                        {
                            yield return pair;
                        }

                        // Re set up
                        LoggingUtil.logLevel = newLogLevel;
                        ConfiguredContract.currentContract = templateContract;
                    }
                }
                finally
                {
                    LoggingUtil.logLevel = origLogLevel;
                    ConfiguredContract.currentContract = null;
                }

                // Store unique data
                foreach (string key in selectedContractType.uniqueValues.Keys)
                {
                    templateContract.uniqueData[key] = selectedContractType.dataNode[key];
                }

                // Check the requirements for our selection
                if (selectedContractType.MeetExtendedRequirements(templateContract) && templateContract.Initialize(selectedContractType))
                {
                    yield return new KeyValuePair<ConfiguredContract, bool>(templateContract, true);
                    yield break;
                }
                // Remove the selection, and try again
                else
                {
                    LoggingUtil.LogVerbose(this, selectedContractType.name + " was not generated: requirement not met.");
                    validContractTypes.Remove(selectedContractType);
                    totalWeight -= selectedContractType.weight;
                    templateContract.uniqueData.Clear();
                    templateContract.contractType = null;
                }

                // Take a pause
                yield return null;
            }
        }

        private ConfiguredContract GetNextContract(Contract.ContractPrestige prestige, bool timeLimited)
        {
            // First try to get one off the queue
            float start = UnityEngine.Time.realtimeSinceStartup;
            while (contractDetails[prestige].contracts.Count > 0)
            {
                ConfiguredContract contract = contractDetails[prestige].contracts.Dequeue();

                if (contract.contractType.MeetRequirements(contract))
                {
                    return contract;
                }

                if (timeLimited && UnityEngine.Time.realtimeSinceStartup - start > MAX_TIME)
                {
                    return null;
                }
            }

            // Check if there's any point in attempting to generate
            LoggingUtil.LogVerbose(this, "   Nothing waiting for GetNextContract, last generation failure was: " +
                (UnityEngine.Time.realtimeSinceStartup - contractDetails[prestige].lastGenerationFailure) + " seconds ago.");
            if (UnityEngine.Time.realtimeSinceStartup - contractDetails[prestige].lastGenerationFailure <= FAILURE_WAIT_TIME)
            {
                LoggingUtil.LogVerbose(this, "   Not going to generate a contract...");
                return null;
            }

            // Try to generate a new contract
            LoggingUtil.LogVerbose(this, "   Attempting to generate new contract");
            foreach (KeyValuePair<ConfiguredContract, bool>? pair in ContractGenerator(prestige))
            {
                if (pair != null && pair.Value.Value)
                {
                    return pair.Value.Key;
                }

                if (timeLimited && UnityEngine.Time.realtimeSinceStartup - start > MAX_TIME)
                {
                    LoggingUtil.LogVerbose(this, "   Timeout generating contract...");
                    if (UnityEngine.Time.realtimeSinceStartup - start > 0.1)
                    {
                        LoggingUtil.LogDebug(this, "Contract attribute took too long (" + (UnityEngine.Time.realtimeSinceStartup - start) +
                            " seconds) to generate: " + lastKey);
                    }
                    return null;
                }
            }

            // Failed to generate a contract
            LoggingUtil.LogVerbose(this, "   Couldn't generate a new contract!");
            contractDetails[prestige].lastGenerationFailure = UnityEngine.Time.realtimeSinceStartup;
            return null;
        }

        public bool GenerateContract(ConfiguredContract contract)
        {
            LoggingUtil.LogVerbose(this, "Request to generate contract of prestige level " + contract.Prestige);
            ConfiguredContract templateContract = GetNextContract(contract.Prestige, HighLogic.LoadedScene == GameScenes.FLIGHT);

            if (templateContract == null)
            {
                return false;
            }

            // Copy the contract details
            contract.CopyFrom(templateContract);
            return true;
        }

        public override void OnSave(ConfigNode node)
        {
            foreach (ContractDetails cd in contractDetails.Values)
            {
                foreach (ConfiguredContract contract in cd.contracts)
                {
                    ConfigNode child = new ConfigNode("CONTRACT");
                    node.AddNode(child);
                    contract.Save(child);
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            foreach (ConfigNode child in node.GetNodes("CONTRACT"))
            {
                ConfiguredContract contract = new ConfiguredContract();
                Contract.Load(contract, child);

                contractDetails[contract.Prestige].contracts.Enqueue(contract);
            }
        }
    }
}
