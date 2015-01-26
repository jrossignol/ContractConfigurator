using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Agents;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for capturing all contract type details.
    /// </summary>
    public class ContractType : IContractConfiguratorFactory
    {
        private static Dictionary<string, ContractType> contractTypes = new Dictionary<string,ContractType>();
        public static IEnumerable<ContractType> AllContractTypes { get { return contractTypes.Values; } }
        public static IEnumerable<ContractType> AllValidContractTypes
        {
            get
            {
                return contractTypes.Values.Where(ct => ct.enabled);
            }
        }
        public static ContractType GetContractType(string name)
        {
            if (contractTypes.ContainsKey(name) && contractTypes[name].enabled)
            {
                return contractTypes[name];
            }
            return null;
        }
        public static void ClearContractTypes()
        {
            contractTypes.Clear();
        }

        protected virtual List<ParameterFactory> paramFactories { get; set; }
        protected virtual List<BehaviourFactory> behaviourFactories { get; set; }
        protected virtual List<ContractRequirement> requirements { get; set; }

        public IEnumerable<ParameterFactory> ParamFactories { get { return paramFactories; } }
        public IEnumerable<BehaviourFactory> BehaviourFactories { get { return behaviourFactories; } }
        public IEnumerable<ContractRequirement> Requirements { get { return requirements; } }

        public bool expandInDebug = false;
        public string config = "";
        public string log;
        public bool enabled { get; private set; }

        // Contract attributes
        public string name;
        public ContractGroup group;
        public string title;
        public string tag;
        public string notes;
        public string description;
        public string topic;
        public string subject;
        public string motivation;
        public string synopsis;
        public string completedMessage;
        public Agent agent;
        public float minExpiry;
        public float maxExpiry;
        public float deadline;
        public bool cancellable;
        public bool declinable;
        public Contract.ContractPrestige? prestige;
        public CelestialBody targetBody;
        public int maxCompletions;
        public int maxSimultaneous;
        public float rewardScience;
        public float rewardReputation;
        public float rewardFunds;
        public float failureReputation;
        public float failureFunds;
        public float advanceFunds;
        public double weight;

        public ContractType(string name)
        {
            this.name = name;
            contractTypes.Add(name, this);

            // Member defaults
            group = null;
            agent = null;
            minExpiry = 0;
            maxExpiry = 0;
            deadline = 0;
            cancellable = true;
            declinable = true;
            prestige = null;
            targetBody = null;
            maxCompletions = 0;
            maxSimultaneous = 0;
            rewardScience = 0.0f;
            rewardReputation = 0.0f;
            rewardFunds = 0.0f;
            failureReputation = 0.0f;
            failureFunds = 0.0f;
            advanceFunds = 0.0f;
            weight = 1.0;
            enabled = true;
        }

        /// <summary>
        /// Loads the contract type details from the given config node.
        /// </summary>
        /// <param name="configNode">The config node to load from.</param>
        /// <returns>Whether the load was successful.</returns>
        public bool Load(ConfigNode configNode)
        {
            // Logging on
            LoggingUtil.CaptureLog = true;

            ConfigNodeUtil.ClearFoundCache();
            bool valid = true;

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", ref name, this);

            // Load contract text details
            valid &= ConfigNodeUtil.ParseValue<ContractGroup>(configNode, "group", ref group, this, (ContractGroup)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", ref title, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "tag", ref tag, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "description", ref description, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "topic", ref topic, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "subject", ref subject, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "motivation", ref motivation, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "notes", ref notes, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "synopsis", ref synopsis, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completedMessage", ref completedMessage, this);

            // Load optional attributes
            valid &= ConfigNodeUtil.ParseValue<Agent>(configNode, "agent", ref agent, this, (Agent)null);
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minExpiry", ref minExpiry, this, 1.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxExpiry", ref maxExpiry, this, 7.0f, x => Validation.GE(x, minExpiry));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "deadline", ref deadline, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "cancellable", ref cancellable, this, true);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "declinable", ref declinable, this, true);
            valid &= ConfigNodeUtil.ParseValue<Contract.ContractPrestige?>(configNode, "prestige", ref prestige, this, (Contract.ContractPrestige?)null);
            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", ref targetBody, this, (CelestialBody)null);
            
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCompletions", ref maxCompletions, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxSimultaneous", ref maxSimultaneous, this, 0, x => Validation.GE(x, 0));

            // Load rewards
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardFunds", ref rewardFunds, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardReputation", ref rewardReputation, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardScience", ref rewardScience, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureFunds", ref failureFunds, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureReputation", ref failureReputation, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "advanceFunds", ref advanceFunds, this, 0.0f, x => Validation.GE(x, 0.0f));

            // Load other values
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "weight", ref weight, this, 1.0, x => Validation.GT(x, 0.0f));
            
            // Check for unexpected values - always do this last
            valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, this);

            log = LoggingUtil.capturedLog;
            LoggingUtil.CaptureLog = false;

            // Load parameters
            paramFactories = new List<ParameterFactory>();
            foreach (ConfigNode contractParameter in configNode.GetNodes("PARAMETER"))
            {
                ParameterFactory paramFactory = null;
                valid &= ParameterFactory.GenerateParameterFactory(contractParameter, this, out paramFactory);
                if (paramFactory != null)
                {
                    paramFactories.Add(paramFactory);
                }
            }

            // Load behaviours
            behaviourFactories = new List<BehaviourFactory>();
            foreach (ConfigNode requirementNode in configNode.GetNodes("BEHAVIOUR"))
            {
                BehaviourFactory behaviourFactory = null;
                valid &= BehaviourFactory.GenerateBehaviourFactory(requirementNode, this, out behaviourFactory);
                if (behaviourFactory != null)
                {
                    behaviourFactories.Add(behaviourFactory);
                }
            }

            // Load requirements
            requirements = new List<ContractRequirement>();
            foreach (ConfigNode requirementNode in configNode.GetNodes("REQUIREMENT"))
            {
                ContractRequirement requirement = null;
                valid &= ContractRequirement.GenerateRequirement(requirementNode, this, out requirement);
                if (requirement != null)
                {
                    requirements.Add(requirement);
                }
            }

            // Logging on
            LoggingUtil.CaptureLog = true;

            // Check we have at least one valid parameter
            if (paramFactories.Count() == 0)
            {
                LoggingUtil.LogError(this.GetType(), ErrorPrefix() + ": Need at least one parameter for a contract!");
                valid = false;
            }

            config = configNode.ToString();
            enabled = valid;
            log += LoggingUtil.capturedLog;
            LoggingUtil.CaptureLog = false;

            return valid;
        }

        /// <summary>
        /// Generates and loads all the parameters required for the given contract.
        /// </summary>
        /// <param name="contract"></param>
        public void GenerateBehaviours(ConfiguredContract contract)
        {
            BehaviourFactory.GenerateBehaviours(contract, behaviourFactories);
        }

        /// <summary>
        /// Generates and loads all the parameters required for the given contract.
        /// </summary>
        /// <param name="contract">Contract to load parameters for</param>
        public void GenerateParameters(ConfiguredContract contract)
        {
            ParameterFactory.GenerateParameters(contract, contract, paramFactories);
        }

        /// <summary>
        /// Tests whether a contract can be offered.
        /// </summary>
        /// <param name="contract">The contract</param>
        /// <returns>Whether the contract can be offered.</returns>
        public bool MeetRequirements(ConfiguredContract contract)
        {
            // Check prestige
            if (prestige != null && contract.Prestige != prestige)
            {
                return false;
            }

            // Get the count of active contracts - excluding ours
            int activeContracts = ContractSystem.Instance.GetCurrentContracts<ConfiguredContract>().Count(c => c.contractType == this);
            if (contract.ContractState == Contract.State.Offered || contract.ContractState == Contract.State.Active)
            {
                activeContracts--;
            }

            // Check if we're breaching the active limit
            if (maxSimultaneous != 0 && activeContracts >= maxSimultaneous)
            {
                return false;
            }

            // Check if we're breaching the completed limit
            if (maxCompletions != 0)
            {
                int finishedContracts = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().Count(c => c.contractType == this);
                if (finishedContracts + activeContracts >= maxCompletions)
                {
                    return false;
                }
            }

            // Check the group values
            if (group != null)
            {
                // Check the group active limit
                activeContracts = ContractSystem.Instance.GetCurrentContracts<ConfiguredContract>().Count(c => c.contractType.group == group);
                if (group.maxSimultaneous != 0 && activeContracts >= group.maxSimultaneous)
                {
                    return false;
                }

                // Check the group completed limit
                if (group.maxCompletions != 0)
                {
                    int finishedContracts = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().Count(c => c.contractType.group == group);
                    if (finishedContracts + activeContracts >= maxCompletions)
                    {
                        return false;
                    }
                }
            }

            // Check the captured requirements
            return ContractRequirement.RequirementsMet(contract, this, requirements);
        }

        /// <summary>
        /// Gets the identifier for the contract type.
        /// </summary>
        /// <returns>String for the contract type.</returns>
        public override string ToString()
        {
            return "CONTRACT_TYPE [" + name + "]";
        }
        
        public string ErrorPrefix()
        {
            return "CONTRACT_TYPE '" + name + "'";
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return ErrorPrefix();
        }

    }
}
