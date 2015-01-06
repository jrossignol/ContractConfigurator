using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    /*
     * Class for capturing a requirement for making a contract available.
     */
    public abstract class ContractRequirement : IContractConfiguratorFactory
    {
        private static Dictionary<string, Type> requirementTypes = new Dictionary<string, Type>();

        protected string name;
        protected string type;
        protected virtual List<ContractRequirement> childNodes { get; set; }
        protected virtual ContractType contractType { get; set; }
        protected CelestialBody targetBody;
        protected bool invertRequirement;
        protected bool checkOnActiveContract;

        /*
         * Loads the ContractRequirement from the given ConfigNode.  The base version loads the following:
         *     - child nodes
         *     - invertRequirement
         */
        public virtual bool Load(ConfigNode configNode)
        {
            bool valid = true;

            // Get name and type
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", ref name, this, "unknown");
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "type", ref type, this);

            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", ref targetBody, this, contractType.targetBody);

            // By default, do not check the requirement for active contracts
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "checkOnActiveContract", ref checkOnActiveContract, this, false);

            // Load invertRequirement flag
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", ref invertRequirement, this, false);

            // Load child nodes
            childNodes = new List<ContractRequirement>();
            foreach (ConfigNode childNode in configNode.GetNodes("REQUIREMENT"))
            {
                ContractRequirement child = ContractRequirement.GenerateRequirement(childNode, contractType);
                if (child != null)
                {
                    childNodes.Add(child);
                }
                else
                {
                    valid = false;
                }
            }

            return valid;
        }

        /*
         * Method for checking whether a contract meets the requirement to be offered.  When called
         * it should check whether the requirement is met.  The passed contract can be used as part
         * of the validation.
         * 
         * If child requirements are supported, then the class implementing this method is
         * responsible for checking those requirements.
         */
        public virtual bool RequirementMet(ConfiguredContract contract) { return true; }

        /*
         * Checks if all the given ContractRequirement meet the requirement.
         */
        public static bool RequirementsMet(ConfiguredContract contract, List<ContractRequirement> contractRequirements)
        {
            bool allReqMet = true;
            foreach (ContractRequirement requirement in contractRequirements)
            {
                if (requirement.checkOnActiveContract || contract.ContractState != Contract.State.Active)
                {
                    bool nodeMet = requirement.RequirementMet(contract);
                    allReqMet = allReqMet && (requirement.invertRequirement ? !nodeMet : nodeMet);
                }
            }

            // Force fail the contract if a requirement becomes unmet
            if(contract.ContractState == Contract.State.Active && !allReqMet)
            {
                // Fail the contract - unfortunately, the player won't know why. :(
                contract.Fail();

                // Force the stock contracts window to refresh
                GameEvents.Contract.onContractsLoaded.Fire();
            }

            return allReqMet;
        }

        /*
         * Adds a new ContractRequirement to handle REQUIREMENT nodes with the given type.
         */
        public static void Register(Type crType, string typeName)
        {
            LoggingUtil.LogDebug(typeof(ContractRequirement), "Registering ContractRequirement class " +
                crType.FullName + " for handling REQUIREMENT nodes with type = " + typeName + ".");

            if (requirementTypes.ContainsKey(typeName))
            {
                LoggingUtil.LogError(typeof(ContractRequirement), "Cannot register " + crType.FullName + "[" + crType.Module +
                    "] to handle type " + typeName + ": already handled by " +
                    requirementTypes[typeName].FullName + "[" +
                    requirementTypes[typeName].Module + "]");
            }
            else
            {
                requirementTypes.Add(typeName, crType);
            }
        }

        /*
         * Generates a ContractRequirement from a configuration node.
         */
        public static ContractRequirement GenerateRequirement(ConfigNode configNode, ContractType contractType)
        {
            // Get the type
            string type = configNode.GetValue("type");
            if (!requirementTypes.ContainsKey(type))
            {
                LoggingUtil.LogError(typeof(ContractRequirement), "No ContractRequirement has been registered for type '" + type + "'.");
                return null;
            }

            // Create an instance of the ContractRequirement
            ContractRequirement requirement = (ContractRequirement)Activator.CreateInstance(requirementTypes[type]);

            // Set attributes
            requirement.contractType = contractType;
            requirement.targetBody = contractType.targetBody;


            // Load config
            bool valid = requirement.Load(configNode);

            // Check for unexpected values - always do this last
            valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, requirement);

            return valid ? requirement : null;
        }

        public string ErrorPrefix()
        {
            return (contractType != null ? "CONTRACT_TYPE '" + contractType.name + "', " : "") + 
                "REQUIREMENT '" + name + "' of type '" + type + "'";
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return (contractType != null ? "CONTRACT_TYPE '" + contractType.name + "', " : "") + 
                "REQUIREMENT '" + configNode.GetValue("name") + "' of type '" + configNode.GetValue("type") + "'";
        }

        /*
         * Validates whether the targetBody valuehas been loaded.
         */
        protected virtual bool ValidateTargetBody(ConfigNode configNode)
        {
            if (targetBody == null)
            {
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody for " + GetType() + " must be specified.");
                return false;
            }
            return true;
        }
    }
}
