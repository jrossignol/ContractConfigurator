using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Agents;
using ContractConfigurator.ExpressionParser;

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
        public static IEnumerable<string> AllValidContractTypeNames
        {
            get
            {
                return AllValidContractTypes.Select<ContractType, string>(ct => ct.name);
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
        public bool enabled { get; private set; }
        public string config { get; private set; }
        public string log { get; private set; }
        public DataNode dataNode { get; private set; }

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
        public List<Contract.ContractPrestige> prestige;
        public CelestialBody targetBody;
        protected List<CelestialBody> targetBodies;
        protected Vessel targetVessel;
        protected List<Vessel> targetVessels;
        protected ProtoCrewMember targetKerbal;
        protected List<ProtoCrewMember> targetKerbals;
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
            prestige = new List<Contract.ContractPrestige>();
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
            try
            {
                // Logging on
                LoggingUtil.CaptureLog = true;

                dataNode = new DataNode(configNode.GetValue("name"), this);

                ConfigNodeUtil.ClearCache(true);
                ConfigNodeUtil.SetCurrentDataNode(dataNode);
                bool valid = true;

                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", x => name = x, this);

                // Load contract text details
                valid &= ConfigNodeUtil.ParseValue<ContractGroup>(configNode, "group", x => group = x, this, (ContractGroup)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", x => title = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "tag", x => tag = x, this, "");
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "description", x => description = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "topic", x => topic = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "subject", x => subject = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "motivation", x => motivation = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "notes", x => notes = x, this, (string)null);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "synopsis", x => synopsis = x, this);
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completedMessage", x => completedMessage = x, this);

                // Load optional attributes
                valid &= ConfigNodeUtil.ParseValue<Agent>(configNode, "agent", x => agent = x, this, (Agent)null);
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minExpiry", x => minExpiry = x, this, 1.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxExpiry", x => maxExpiry = x, this, 7.0f, x => Validation.GE(x, minExpiry));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "deadline", x => deadline = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "cancellable", x => cancellable = x, this, true);
                valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "declinable", x => declinable = x, this, true);
                valid &= ConfigNodeUtil.ParseValue<List<Contract.ContractPrestige>>(configNode, "prestige", x => prestige = x, this, new List<Contract.ContractPrestige>());
                valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", x => targetBody = x, this, (CelestialBody)null);
                valid &= ConfigNodeUtil.ParseValue<List<CelestialBody>>(configNode, "targetBodies", x => targetBodies = x, this, new List<CelestialBody>());
                valid &= ConfigNodeUtil.ParseValue<Vessel>(configNode, "targetVessel", x => targetVessel = x, this, (Vessel)null);
                valid &= ConfigNodeUtil.ParseValue<List<Vessel>>(configNode, "targetVessels", x => targetVessels = x, this, new List<Vessel>());
                valid &= ConfigNodeUtil.ParseValue<ProtoCrewMember>(configNode, "targetKerbal", x => targetKerbal = x, this, (ProtoCrewMember)null);
                valid &= ConfigNodeUtil.ParseValue<List<ProtoCrewMember>>(configNode, "targetKerbals", x => targetKerbals = x, this, new List<ProtoCrewMember>());
            
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxCompletions", x => maxCompletions = x, this, 0, x => Validation.GE(x, 0));
                valid &= ConfigNodeUtil.ParseValue<int>(configNode, "maxSimultaneous", x => maxSimultaneous = x, this, 0, x => Validation.GE(x, 0));

                // Load rewards
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardFunds", x => rewardFunds = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardReputation", x => rewardReputation = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardScience", x => rewardScience = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureFunds", x => failureFunds = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureReputation", x => failureReputation = x, this, 0.0f, x => Validation.GE(x, 0.0f));
                valid &= ConfigNodeUtil.ParseValue<float>(configNode, "advanceFunds", x => advanceFunds = x, this, 0.0f, x => Validation.GE(x, 0.0f));

                // Load other values
                valid &= ConfigNodeUtil.ParseValue<double>(configNode, "weight", x => weight = x, this, 1.0, x => Validation.GE(x, 0.0f));
            
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

                // Do the deferred loads
                valid &= ConfigNodeUtil.ExecuteDeferredLoads();

                config = configNode.ToString();
                enabled = valid;
                log += LoggingUtil.capturedLog;
                LoggingUtil.CaptureLog = false;

                return valid;
            }
            catch
            {
                enabled = false;
                throw;
            }
        }

        /// <summary>
        /// Generates and loads all the parameters required for the given contract.
        /// </summary>
        /// <param name="contract"></param>
        /// <returns>Whether the generation was successful.</returns>
        public bool GenerateBehaviours(ConfiguredContract contract)
        {
            return BehaviourFactory.GenerateBehaviours(contract, behaviourFactories);
        }

        /// <summary>
        /// Generates and loads all the parameters required for the given contract.
        /// </summary>
        /// <param name="contract">Contract to load parameters for</param>
        /// <returns>Whether the generation was successful.</returns>
        public bool GenerateParameters(ConfiguredContract contract)
        {
            return ParameterFactory.GenerateParameters(contract, contract, paramFactories);
        }

        /// <summary>
        /// Tests whether a contract can be offered.
        /// </summary>
        /// <param name="contract">The contract</param>
        /// <returns>Whether the contract can be offered.</returns>
        public bool MeetRequirements(ConfiguredContract contract)
        {
            // Check prestige
            if (prestige.Count > 0 && !prestige.Contains(contract.Prestige))
            {
                return false;
            }

            // Checks for maxSimultaneous/maxCompletions
            if (maxSimultaneous != 0 || maxCompletions != 0)
            {
                // Get the count of active contracts - excluding ours
                int activeContracts = ContractSystem.Instance.GetCurrentContracts<ConfiguredContract>().Count(c => c.contractType == this);
                if (contract.ContractState == Contract.State.Offered || contract.ContractState == Contract.State.Active)
                {
                    activeContracts--;
                }

                // Check if we're breaching the active limit
                if (maxSimultaneous != 0 && activeContracts >= maxSimultaneous)
                {
                    LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", too many active contracts.");
                    return false;
                }

                // Check if we're breaching the completed limit
                if (maxCompletions != 0)
                {
                    int finishedContracts = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().Count(c => c.contractType == this);
                    if (finishedContracts + activeContracts >= maxCompletions)
                    {
                        LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", too many completed/active/offered contracts.");
                        return false;
                    }
                }
            }

            // Check the group values
            if (group != null)
            {
                // Check the group active limit
                int activeContracts = ContractSystem.Instance.GetCurrentContracts<ConfiguredContract>().Count(c => c.contractType != null && c.contractType.group == group);
                if (contract.ContractState == Contract.State.Offered || contract.ContractState == Contract.State.Active)
                {
                    activeContracts--;
                }
                
                if (group.maxSimultaneous != 0 && activeContracts >= group.maxSimultaneous)
                {
                    LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", too many active contracts in group.");
                    return false;
                }

                // Check the group completed limit
                if (group.maxCompletions != 0)
                {
                    int finishedContracts = ContractSystem.Instance.GetCompletedContracts<ConfiguredContract>().Count(c => c.contractType != null && c.contractType.group == group);
                    if (finishedContracts + activeContracts >= maxCompletions)
                    {
                        LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", too many completed contracts in group.");
                        return false;
                    }
                }
            }

            // Check special values are not null
            if (contract.ContractState != Contract.State.Active)
            {
                // Target Bodies
                if (targetBodies.Count == 0 && !dataNode.IsDeterministic("targetBodies"))
                {
                    LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", targetBodies is empty.");
                    return false;
                }

                // Target Vessel
                if (targetVessel == null && !dataNode.IsDeterministic("targetVessel"))
                {
                    LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", targetVessel is null.");
                    return false;
                }
                if (targetVessels.Count == 0 && !dataNode.IsDeterministic("targetVessels"))
                {
                    LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", targetVessels is empty.");
                    return false;
                }

                // Target Kerbal
                if (targetKerbal == null && !dataNode.IsDeterministic("targetKerbal"))
                {
                    LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", targetKerbal is null.");
                    return false;
                }
                if (targetKerbals.Count == 0 && !dataNode.IsDeterministic("targetKerbals"))
                {
                    LoggingUtil.LogVerbose(this, "Didn't generate contract type " + name + ", targetKerbals is empty.");
                    return false;
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
