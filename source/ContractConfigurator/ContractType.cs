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
    /*
     * Class for capturing all contract type details.
     */
    public class ContractType : IContractConfiguratorFactory
    {
        public static Dictionary<string, ContractType> contractTypes = new Dictionary<string,ContractType>();

        protected virtual List<ParameterFactory> paramFactories { get; set; }
        protected virtual List<BehaviourFactory> behaviourFactories { get; set; }
        protected virtual List<ContractRequirement> requirements { get; set; }

        // Contract attributes
        public string name;
        public string title;
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
        }

        ~ContractType()
        {
            contractTypes.Remove(name);
        }

        /*
         * Loads the contract type details from the given config node.
         */
        public bool Load(ConfigNode configNode)
        {
            ConfigNodeUtil.ClearFoundCache();
            bool valid = true;

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", ref name, this);

            // Load contract text details
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", ref title, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "description", ref description, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "topic", ref topic, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "subject", ref subject, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "motivation", ref motivation, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "notes", ref notes, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "synopsis", ref synopsis, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completedMessage", ref completedMessage, this);

            // Load optional attributes
            valid &= ConfigNodeUtil.ParseValue<Agent>(configNode, "agent", ref agent, this, (Agent)null);
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minExpiry", ref minExpiry, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxExpiry", ref maxExpiry, this, 0.0f, x => Validation.GE(x, 0.0f));
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

            // Load parameters
            paramFactories = new List<ParameterFactory>();
            foreach (ConfigNode contractParameter in configNode.GetNodes("PARAMETER"))
            {
                ParameterFactory paramFactory = ParameterFactory.GenerateParameterFactory(contractParameter, this);
                if (paramFactory != null)
                {
                    paramFactories.Add(paramFactory);
                }
                else
                {
                    valid = false;
                }
            }

            // Check we have at least one valid parameter
            if (paramFactories.Count() == 0)
            {
                LoggingUtil.LogError(this.GetType(), ErrorPrefix() + ": Need at least one parameter for a contract!");
                valid = false;
            }

            // Load behaviours
            behaviourFactories = new List<BehaviourFactory>();
            foreach (ConfigNode requirementNode in configNode.GetNodes("BEHAVIOUR"))
            {
                BehaviourFactory behaviourFactory = BehaviourFactory.GenerateBehaviourFactory(requirementNode, this);
                if (behaviourFactory != null)
                {
                    behaviourFactories.Add(behaviourFactory);
                }
                else
                {
                    valid = false;
                }
            }

            // Load requirements
            requirements = new List<ContractRequirement>();
            foreach (ConfigNode requirementNode in configNode.GetNodes("REQUIREMENT"))
            {
                ContractRequirement requirement = ContractRequirement.GenerateRequirement(requirementNode, this);
                if (requirement != null)
                {
                    requirements.Add(requirement);
                }
                else
                {
                    valid = false;
                }
            }


            return valid;
        }

        /*
         * Generates and loads all the parameters required for the given contract.
         */
        public void GenerateBehaviours(ConfiguredContract contract)
        {
            BehaviourFactory.GenerateBehaviours(contract, behaviourFactories);
        }

        /*
         * Generates and loads all the parameters required for the given contract.
         */
        public void GenerateParameters(ConfiguredContract contract)
        {
            ParameterFactory.GenerateParameters(contract, contract, paramFactories);
        }

        /*
         * Returns true if the contract can be offered.
         */
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

            // Check the captured requirements
            return ContractRequirement.RequirementsMet(contract, this, requirements);
        }

        /*
         * Returns the name of the contract type.
         */
        public override string ToString()
        {
            return "ContractType[" + name + "]";
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
