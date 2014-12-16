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
    public class ContractType
    {
        public static Dictionary<string, ContractType> contractTypes = new Dictionary<string,ContractType>();

        protected virtual List<ParameterFactory> paramFactories { get; set; }
        protected virtual List<BehaviourFactory> behaviourFactories { get; set; }
        protected virtual List<ContractRequirement> requirements { get; set; }

        // Contract attributes
        public virtual string name { get; private set; }
        public virtual string title { get; set; }
        public virtual string notes { get; set; }
        public virtual string description { get; set; }
        public virtual string topic { get; set; }
        public virtual string subject { get; set; }
        public virtual string motivation { get; set; }
        public virtual string synopsis { get; set; }
        public virtual string completedMessage { get; set; }
        public virtual Agent agent { get; set; }
        public virtual float minExpiry { get; set; }
        public virtual float maxExpiry { get; set; }
        public virtual float deadline { get; set; }
        public virtual bool cancellable { get; set; }
        public virtual bool declinable { get; set; }
        public virtual Contract.ContractPrestige? prestige { get; set; }
        public virtual CelestialBody targetBody { get; set; }
        public virtual int maxCompletions { get; set; }
        public virtual int maxSimultaneous { get; set; }
        public virtual float rewardScience { get; set; }
        public virtual float rewardReputation { get; set; }
        public virtual float rewardFunds { get; set; }
        public virtual float failureReputation { get; set; }
        public virtual float failureFunds { get; set; }
        public virtual float advanceFunds { get; set; }
        public virtual double weight { get; set; }

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
        public void Load(ConfigNode contractConfig)
        {
            // Load contract text details
            title = contractConfig.GetValue("title");
            description = contractConfig.GetValue("description");
            topic = contractConfig.GetValue("topic");
            subject = contractConfig.GetValue("subject");
            motivation = contractConfig.GetValue("motivation");
            notes = contractConfig.GetValue("notes");
            synopsis = contractConfig.GetValue("synopsis");
            completedMessage = contractConfig.GetValue("completedMessage");

            // Load optional attributes
            if (contractConfig.HasValue("agent"))
            {
                agent = AgentList.Instance.GetAgent(contractConfig.GetValue("agent"));
                if (agent == null)
                {
                    Debug.LogWarning("ContractConfigurator: No agent with name '" +
                        contractConfig.GetValue("agent") + "'.");
                }
            }
            if (contractConfig.HasValue("minExpiry"))
            {
                minExpiry = (float)Convert.ToDouble(contractConfig.GetValue("minExpiry"));
            }
            if (contractConfig.HasValue("maxExpiry"))
            {
                maxExpiry = (float)Convert.ToDouble(contractConfig.GetValue("maxExpiry"));
            }
            if (contractConfig.HasValue("deadline"))
            {
                deadline = (float)Convert.ToDouble(contractConfig.GetValue("deadline"));
            }
            if (contractConfig.HasValue("cancellable"))
            {
                cancellable = Convert.ToBoolean(contractConfig.GetValue("cancellable"));
            }
            if (contractConfig.HasValue("declinable"))
            {
                declinable = Convert.ToBoolean(contractConfig.GetValue("declinable"));
            }
            if (contractConfig.HasValue("prestige"))
            {
                prestige = (Contract.ContractPrestige)Enum.Parse(typeof(Contract.ContractPrestige),
                    contractConfig.GetValue("prestige"));
            }
            targetBody = ConfigNodeUtil.ParseCelestialBody(contractConfig, "targetBody");

            maxCompletions = Convert.ToInt32(contractConfig.GetValue("maxCompletions"));
            maxSimultaneous = Convert.ToInt32(contractConfig.GetValue("maxSimultaneous"));

            // Load rewards
            rewardFunds = (float)Convert.ToDouble(contractConfig.GetValue("rewardFunds"));
            rewardReputation = (float)Convert.ToDouble(contractConfig.GetValue("rewardReputation"));
            rewardScience = (float)Convert.ToDouble(contractConfig.GetValue("rewardScience"));
            failureFunds = (float)Convert.ToDouble(contractConfig.GetValue("failureFunds"));
            failureReputation = (float)Convert.ToDouble(contractConfig.GetValue("failureReputation"));
            advanceFunds = (float)Convert.ToDouble(contractConfig.GetValue("advanceFunds"));

            // Load other values
            if (contractConfig.HasValue("weight"))
            {
                weight = Convert.ToDouble(contractConfig.GetValue("weight"));
            }

            // Load parameters
            paramFactories = new List<ParameterFactory>();
            foreach (ConfigNode contractParameter in contractConfig.GetNodes("PARAMETER"))
            {
                ParameterFactory paramFactory = ParameterFactory.GenerateParameterFactory(contractParameter, this);
                if (paramFactory != null)
                {
                    paramFactories.Add(paramFactory);
                }
            }

            // Check we have at least one valid parameter
            if (paramFactories.Count() == 0)
            {
                throw new Exception("Need at least one parameter for a contract!");
            }

            // Load behaviours
            behaviourFactories = new List<BehaviourFactory>();
            foreach (ConfigNode requirementNode in contractConfig.GetNodes("BEHAVIOUR"))
            {
                BehaviourFactory behaviourFactory = BehaviourFactory.GenerateBehaviourFactory(requirementNode, this);
                if (behaviourFactory != null)
                {
                    behaviourFactories.Add(behaviourFactory);
                }
            }

            // Load requirements
            requirements = new List<ContractRequirement>();
            foreach (ConfigNode requirementNode in contractConfig.GetNodes("REQUIREMENT"))
            {
                ContractRequirement requirement = ContractRequirement.GenerateRequirement(requirementNode, this);
                if (requirement != null)
                {
                    requirements.Add(requirement);
                }
            }
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
            return ContractRequirement.RequirementsMet(contract, requirements);
        }

        /*
         * Returns the name of the contract type.
         */
        public override string ToString()
        {
            return "ContractType[" + name + "]";
        }
    }
}
