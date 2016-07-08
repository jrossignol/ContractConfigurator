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
        public static EventVoid OnInitializeValues = new EventVoid("OnPreLoaderInitializeValues");
        public static EventVoid OnInitializeFail = new EventVoid("OnPreLoaderInitializeFail");

        public static ContractPreLoader Instance { get; private set; }

        private const int MAX_CONTRACTS = 5;
        private const float MAX_TIME = 0.0075f;
        private const float GLOBAL_FAILURE_WAIT_TIME = 30.0f;
        private const float FAILURE_WAIT_TIME = 60.0f;
        private const float RANDOM_MIN = -15.0f;
        private const float RANDOM_MAX = 30.0f;

        private static System.Random rand = new System.Random();
        private static int nextContractGroup = rand.Next();

        private List<ConfiguredContract> contracts = new List<ConfiguredContract>();

        private string lastKey = null;
        private double lastGenerationFailure;
        private IEnumerator<ConfiguredContract> contractEnumerator;

        public ContractPreLoader()
        {
            Instance = this;
        }

        void Start()
        {
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccept));
            GameEvents.Contract.onFinished.Add(new EventData<Contract>.OnEvent(OnContractFinish));
            GameEvents.Contract.onDeclined.Add(new EventData<Contract>.OnEvent(OnContractDecline));
            GameEvents.OnProgressReached.Add(new EventData<ProgressNode>.OnEvent(OnProgressReached));

            // TODO - on accept and withdraw, clear out the lists
        }

        void OnDestroy()
        {
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccept));
            GameEvents.Contract.onFinished.Remove(new EventData<Contract>.OnEvent(OnContractFinish));
            GameEvents.Contract.onDeclined.Remove(new EventData<Contract>.OnEvent(OnContractDecline));
            GameEvents.OnProgressReached.Remove(new EventData<ProgressNode>.OnEvent(OnProgressReached));
        }

        void OnProgressReached(ProgressNode p)
        {
            // Reset the generation failures
            ResetGenerationFailure();
        }

        void OnContractAccept(Contract c)
        {
            LoggingUtil.LogVerbose(this, "OnContractAccept");

            ConfiguredContract cc = c as ConfiguredContract;
            if (cc != null && cc.preLoaded)
            {
                contracts.Remove(cc);
                ContractSystem.Instance.Contracts.Add(cc);
            }
        }

        void OnContractDecline(Contract c)
        {
            LoggingUtil.LogVerbose(this, "OnContractDecline");

            // Reset generation failures for just this contract type
            ConfiguredContract cc = c as ConfiguredContract;
            if (cc != null)
            {
                if (cc.preLoaded)
                {
                    contracts.Remove(cc);
                }
                if (cc.contractType != null)
                {
                    lastGenerationFailure = 0.0;
                    cc.contractType.failedGenerationAttempts = 0;
                    cc.contractType.lastGenerationFailure = 0.0;
                }
            }
        }

        void OnContractFinish(Contract c)
        {
            LoggingUtil.LogVerbose(this, "OnContractFinish");

            // Reset the generation failures for all contract types
            ResetGenerationFailure();
        }

        /// <summary>
        /// Static method to be used via reflection to force a contract generation pass.
        /// </summary>
        public static void ForceContractGenerationPass()
        {
            if (Instance != null)
            {
                Instance.ResetGenerationFailure();
            }
        }

        public void ResetGenerationFailure()
        {
            LoggingUtil.LogVerbose(this, "ResetGenerationFailure");

            lastGenerationFailure = 0.0;
            foreach (ContractType ct in ContractType.AllValidContractTypes)
            {
                ct.failedGenerationAttempts = 0;
                ct.lastGenerationFailure = 0.0;
            }
        }

        void Update()
        {
            // Wait for startup of contract system
            if (ContractSystem.Instance == null)
            {
                return;
            }

            if (contractEnumerator == null)
            {
                contractEnumerator = ContractEnumerator().GetEnumerator();
            }

            // Loop through the enumerator until we run out of time, hit the end or generate a contract
            float start = UnityEngine.Time.realtimeSinceStartup;
            int count = 0;
            while (UnityEngine.Time.realtimeSinceStartup - start < MAX_TIME && lastGenerationFailure + GLOBAL_FAILURE_WAIT_TIME < Time.realtimeSinceStartup)
            {
                count++;
                if (!contractEnumerator.MoveNext())
                {
                    // We ran through the entire enumerator, mark the failure
                    LoggingUtil.LogVerbose(this, "Contract generation failure");
                    lastGenerationFailure = UnityEngine.Time.realtimeSinceStartup + (float)(rand.NextDouble() * (RANDOM_MAX - RANDOM_MIN) + RANDOM_MIN);
                    contractEnumerator = null;
                    break;
                }

                ConfiguredContract contract = contractEnumerator.Current;
                if (contract != null)
                {
                    // If the contract is auto-accept, add it immediately
                    if (contract.contractType.autoAccept)
                    {
                        ContractSystem.Instance.Contracts.Add(contract);
                        contract.Accept();
                    }
                    else
                    {
                        contract.preLoaded = true;
                        contracts.Add(contract);
                    }

                    contractEnumerator = null;
                    break;
                }
            }

            if (UnityEngine.Time.realtimeSinceStartup - start > 0.1)
            {
                LoggingUtil.LogDebug(this, "Contract attribute took too long (" + (UnityEngine.Time.realtimeSinceStartup - start) +
                    " seconds) to generate: " + lastKey);
            }
        }

        private IEnumerable<ConfiguredContract> ContractEnumerator()
        {
            // Loop through all the contract groups
            IEnumerable<ContractGroup> groups = ContractGroup.AllGroups;
            for (int i = 0; i < groups.Count(); i++)
            {
                nextContractGroup = (nextContractGroup + 1) % groups.Count();
                ContractGroup group = groups.ElementAt(nextContractGroup);

                foreach (ContractType ct in ContractType.AllValidContractTypes)
                {
                    // Is the contract time part of this group, and is it allowed to attempt to generate
                    if (ct.group == group && ct.lastGenerationFailure + FAILURE_WAIT_TIME < Time.realtimeSinceStartup)
                    {
                        if (ct.lastGenerationFailure != -100)
                        {
                            ct.lastGenerationFailure = -100;
                            ct.failedGenerationAttempts = 0;
                        }

                        // Are we in the right scene, or is is a special contract that can generate in any scene
                        if (HighLogic.LoadedScene == GameScenes.SPACECENTER || ct.autoAccept)
                        {
                            foreach (ConfiguredContract contract in GenerateContract(ct))
                            {
                                yield return contract;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<ConfiguredContract> GenerateContract(ContractType contractType)
        {
            // TODO - fix up expiry date handling


            ConfiguredContract templateContract = Contract.Generate(typeof(ConfiguredContract), Contract.ContractPrestige.Trivial, rand.Next(), Contract.State.Withdrawn) as ConfiguredContract;

            // First, check the basic requirements
            if (!contractType.MeetBasicRequirements(templateContract))
            {
                LoggingUtil.LogVerbose(this, contractType.name + " was not generated: basic requirements not met.");
                if (++contractType.failedGenerationAttempts >= contractType.maxConsecutiveGenerationFailures)
                {
                    contractType.lastGenerationFailure = Time.realtimeSinceStartup;
                }

                yield return null;
                yield break;
            }

            // Try to refresh non-deterministic values before we check extended requirements
            OnInitializeValues.Fire();
            LoggingUtil.LogLevel origLogLevel = LoggingUtil.logLevel;
            LoggingUtil.LogLevel newLogLevel = contractType.trace ? LoggingUtil.LogLevel.VERBOSE : LoggingUtil.logLevel;
            try
            {
                // Set up for loop
                LoggingUtil.logLevel = newLogLevel;
                ConfiguredContract.currentContract = templateContract;

                // Set up the iterator to refresh non-deterministic values
                IEnumerable<string> iter = ConfigNodeUtil.UpdateNonDeterministicValuesIterator(contractType.dataNode);
                for (ContractGroup g = contractType.group; g != null; g = g.parent)
                {
                    iter = ConfigNodeUtil.UpdateNonDeterministicValuesIterator(g.dataNode).Concat(iter);
                }

                // Update the actual values
                LoggingUtil.LogVerbose(this, "Refresh non-deterministic values for CONTRACT_TYPE = " + contractType.name);
                foreach (string val in iter)
                {
                    lastKey = contractType.name + "[" + val + "]";

                    // Clear temp stuff
                    LoggingUtil.logLevel = origLogLevel;
                    ConfiguredContract.currentContract = null;

                    if (val == null)
                    {
                        LoggingUtil.LogVerbose(this, contractType.name + " was not generated: non-deterministic expression failure.");
                        if (++contractType.failedGenerationAttempts >= contractType.maxConsecutiveGenerationFailures)
                        {
                            contractType.lastGenerationFailure = Time.realtimeSinceStartup;
                        }

                        OnInitializeFail.Fire();
                        yield return null;
                        yield break;
                    }
                    else
                    {
                        // Quick pause
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

            // Store unique data
            foreach (string key in contractType.uniquenessChecks.Keys)
            {
                templateContract.uniqueData[key] = contractType.dataNode[key];
            }

            // Set the desired prestige
            int t1, t2, t3;
            ContractSystem.GetContractCounts(Reputation.CurrentRep, 1000, out t1, out t2, out t3);
            if (contractType.prestige.Any())
            {
                if (!contractType.prestige.Contains(Contract.ContractPrestige.Trivial))
                {
                    t1 = 0;
                }
                if (!contractType.prestige.Contains(Contract.ContractPrestige.Significant))
                {
                    t2 = 0;
                }
                if (!contractType.prestige.Contains(Contract.ContractPrestige.Exceptional))
                {
                    t3 = 0;
                }
            }
            int selection = rand.Next(0, t1 + t2 + t3);
            templateContract.Prestige = selection < t1 ? Contract.ContractPrestige.Trivial : selection < t2 ? Contract.ContractPrestige.Significant : Contract.ContractPrestige.Exceptional;

            // Check the requirements for our selection
            if (contractType.MeetExtendedRequirements(templateContract, contractType) && templateContract.Initialize(contractType))
            {
                templateContract.ContractState = Contract.State.Offered;
                yield return templateContract;
            }
            // Failure, add a pause in before finishing
            else
            {
                LoggingUtil.LogVerbose(this, contractType.name + " was not generated: requirement not met.");
                if (++contractType.failedGenerationAttempts >= contractType.maxConsecutiveGenerationFailures)
                {
                    contractType.lastGenerationFailure = Time.realtimeSinceStartup;
                }

                OnInitializeFail.Fire();
                yield return null;
            }
        }

        private ConfiguredContract GetNextContract(Contract.ContractPrestige prestige, bool timeLimited)
        {
            // No contracts, no point
            if (!contracts.Any())
            {
                return null;
            }

            // Try to get a contract that matches the request
            float start = UnityEngine.Time.realtimeSinceStartup;
            int startVal = rand.Next(0, contracts.Count);
            for (int i = contracts.Count - 1; i >= 0; i--)
            {
                int index = (startVal + i) % contracts.Count;
                ConfiguredContract contract = contracts[index];
                contracts.RemoveAt(index);

                if (contract.contractType != null && contract.contractType.MeetRequirements(contract, contract.contractType))
                {
                    return contract;
                }
            
                if (timeLimited && UnityEngine.Time.realtimeSinceStartup - start > MAX_TIME)
                {
                    return null;
                }
            }

            // No contracts
            return null;
        }

        public bool GenerateContract(ConfiguredContract contract)
        {
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
                foreach (ConfiguredContract contract in contracts)
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
                        contract.preLoaded = true;
                        contracts.Add(contract);
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

        public IEnumerable<ConfiguredContract> PendingContracts()
        {
            return contracts;
        }

        public IEnumerable<ConfiguredContract> PendingContracts(Contract.ContractPrestige? prestige = null)
        {
            return contracts.Where(c => prestige == c.Prestige);
        }

        public IEnumerable<ConfiguredContract> PendingContracts(ContractType type, Contract.ContractPrestige? prestige = null)
        {
            if (type == null)
            {
                return Enumerable.Empty<ConfiguredContract>();
            }

            return contracts.Where(c => c.contractType == type && (prestige == null || prestige == c.Prestige));
        }
    }
}
