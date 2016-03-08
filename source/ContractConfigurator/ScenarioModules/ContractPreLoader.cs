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

        public static EventVoid OnInitializeValues = new EventVoid("OnPreLoaderInitializeValues");
        public static EventVoid OnInitializeFail = new EventVoid("OnPreLoaderInitializeFail");

        public static ContractPreLoader Instance { get; private set; }

        private const int MAX_CONTRACTS = 5;
        private const float MAX_TIME = 0.01f;
        private const float FAILURE_WAIT_TIME = 30.0f;

        private static int nextContractGroup = 0;
        System.Random rand = new System.Random();

        private Dictionary<Contract.ContractPrestige, ContractDetails> contractDetails = new Dictionary<Contract.ContractPrestige, ContractDetails>();
        private Queue<ConfiguredContract> priorityContracts = new Queue<ConfiguredContract>();
        private IEnumerator<ConfiguredContract> currentEnumerator = null;
        private ContractDetails currentDetails = null;

        private string lastKey = null;

        private static MethodInfo generateContractMethod = typeof(ContractSystem).GetMethod("GenerateContracts", BindingFlags.Instance | BindingFlags.NonPublic);

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

        public void ResetGenerationFailure()
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
                    (cd.contracts.Count() < MAX_CONTRACTS || ContractType.AllValidContractTypes.Any(ct => ct.autoAccept && ct.prestige.Contains(cd.prestige))));

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

                ConfiguredContract contract = currentEnumerator.Current;
                if (contract != null)
                {
                    (contract.contractType.autoAccept ? priorityContracts : currentDetails.contracts).Enqueue(contract);

                    // We generated a high priority contract...  force the system to do a generation pass
                    if (contract.contractType.autoAccept)
                    {
                        generateContractMethod.Invoke(ContractSystem.Instance, new object[] { rand.Next(int.MaxValue), contract.Prestige, 1 });
                    }

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

        private IEnumerable<ConfiguredContract> ContractGenerator(Contract.ContractPrestige prestige)
        {
            // Loop through all the contract groups
            IEnumerable<ContractGroup> groups = ContractGroup.AllGroups;
            foreach (ContractGroup group in groups.Skip(nextContractGroup).Concat(groups.Take(nextContractGroup)))
            {
                nextContractGroup = (nextContractGroup + 1) % groups.Count();

                foreach (ConfiguredContract c in ContractGenerator(prestige, group))
                {
                    yield return c;
                }
            }
        }

        private IEnumerable<ConfiguredContract> ContractGenerator(Contract.ContractPrestige prestige, ContractGroup group)
        {
            ConfiguredContract templateContract =
                Contract.Generate(typeof(ConfiguredContract), prestige, rand.Next(), Contract.State.Withdrawn) as ConfiguredContract;

            // Build a weighted list of ContractTypes to choose from
            Dictionary<ContractType, double> validContractTypes = new Dictionary<ContractType, double>();
            double totalWeight = 0.0;
            foreach (ContractType ct in ContractType.AllValidContractTypes.Where(ct => ct.group == group))
            {
                // Check if we're only looking at auto-accept contracts
                if (currentDetails != null && currentDetails.contracts.Count() >= MAX_CONTRACTS && !ct.autoAccept)
                {
                    continue;
                }

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
                OnInitializeValues.Fire();
                LoggingUtil.LogLevel origLogLevel = LoggingUtil.logLevel;
                LoggingUtil.LogLevel newLogLevel = selectedContractType.trace ? LoggingUtil.LogLevel.VERBOSE : LoggingUtil.logLevel;
                bool failure = false;
                try
                {
                    // Set up for loop
                    LoggingUtil.logLevel = newLogLevel;
                    ConfiguredContract.currentContract = templateContract;

                    // Set up the iterator to refresh non-deterministic values
                    IEnumerable<string> iter = ConfigNodeUtil.UpdateNonDeterministicValuesIterator(selectedContractType.dataNode);
                    for (ContractGroup g = selectedContractType.group; g != null; g = g.parent)
                    {
                        iter = ConfigNodeUtil.UpdateNonDeterministicValuesIterator(g.dataNode).Concat(iter);
                    }

                    // Update the actual values
                    LoggingUtil.LogVerbose(this, "Refresh non-deterministic values for CONTRACT_TYPE = " + selectedContractType.name);
                    foreach (string val in iter)
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
                            failure = true;
                            break;
                        }
                        else
                        {
                            yield return null;
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

                if (failure)
                {
                    OnInitializeFail.Fire();
                    continue;
                }

                // Store unique data
                foreach (string key in selectedContractType.uniquenessChecks.Keys)
                {
                    templateContract.uniqueData[key] = selectedContractType.dataNode[key];
                }

                // Check the requirements for our selection
                if (selectedContractType.MeetExtendedRequirements(templateContract, selectedContractType) && templateContract.Initialize(selectedContractType))
                {
                    yield return templateContract;
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
                OnInitializeFail.Fire();
                yield return null;
            }
        }

        private ConfiguredContract GetNextContract(Contract.ContractPrestige prestige, bool timeLimited)
        {
            // First try to get one off the queue
            float start = UnityEngine.Time.realtimeSinceStartup;
            while (priorityContracts.Any() || contractDetails[prestige].contracts.Count > 0)
            {
                ConfiguredContract contract = (priorityContracts.Any() ? priorityContracts : contractDetails[prestige].contracts).Dequeue();

                if (contract != null && contract.contractType != null && contract.contractType.MeetRequirements(contract, contract.contractType))
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
            foreach (ConfiguredContract contract in ContractGenerator(prestige))
            {
                if (contract != null)
                {
                    return contract;
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
            try
            {
                foreach (ConfiguredContract contract in contractDetails.Values.SelectMany(cd => cd.contracts).Union(priorityContracts))
                {
                    ConfigNode child = new ConfigNode("CONTRACT");
                    node.AddNode(child);
                    contract.Save(child);
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error saving ContractPreLoader to persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_SAVE, e, "ContractPreLoader");
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                foreach (ConfigNode child in node.GetNodes("CONTRACT"))
                {
                    ConfiguredContract contract = null;
                    try
                    {
                        contract = new ConfiguredContract();
                        Contract.Load(contract, child);
                    }
                    catch (Exception e)
                    {
                        LoggingUtil.LogWarning(this, "Ignored an exception while trying to load a pre-loaded contract:");
                        LoggingUtil.LogException(e);
                    }

                    if (contract != null && contract.contractType != null)
                    {
                        if (contract.contractType.autoAccept)
                        {
                            priorityContracts.Enqueue(contract);
                        }
                        else
                        {
                            contractDetails[contract.Prestige].contracts.Enqueue(contract);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error loading ContractPreLoader from persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_LOAD, e, "ContractPreLoader");
            }
        }

        public IEnumerable<ConfiguredContract> PendingContracts(Contract.ContractPrestige? prestige = null)
        {
            return contractDetails.SelectMany(p => p.Value.contracts).Union(priorityContracts).
                Where(c => prestige == null || prestige == c.Prestige);
        }

        public IEnumerable<ConfiguredContract> PendingContracts(ContractType type, Contract.ContractPrestige? prestige = null)
        {
            if (type == null)
            {
                return Enumerable.Empty<ConfiguredContract>();
            }

            return contractDetails.SelectMany(p => p.Value.contracts).Union(priorityContracts).
                Where(c => c.contractType == type && (prestige == null || prestige == c.Prestige));
        }
    }
}
