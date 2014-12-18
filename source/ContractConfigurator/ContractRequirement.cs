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
    public abstract class ContractRequirement
    {
        private static Dictionary<string, Type> requirementTypes = new Dictionary<string, Type>();

        protected virtual List<ContractRequirement> childNodes { get; set; }
        protected virtual ContractType contractType { get; set; }
        protected virtual CelestialBody targetBody { get; set; }
        protected virtual bool invertRequirement { get; set; }

        /*
         * Loads the ContractRequirement from the given ConfigNode.  The base version loads the following:
         *     - child nodes
         *     - invertRequirement
         */
        public virtual bool Load(ConfigNode configNode)
        {
            bool valid = true;

            // Load invertRequirement flag
            invertRequirement = false;
            if (configNode.HasValue("invertRequirement"))
            {
                invertRequirement = Convert.ToBoolean(configNode.GetValue("invertRequirement"));
            }

            // Load child nodes
            childNodes = new List<ContractRequirement>();
            foreach (ConfigNode childNode in configNode.GetNodes("REQUIREMENT"))
            {
                ContractRequirement child = ContractRequirement.GenerateRequirement(childNode, contractType);
                if (child != null)
                {
                    childNodes.Add(child);
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
                bool nodeMet = requirement.RequirementMet(contract);
                allReqMet = allReqMet && (requirement.invertRequirement ? !nodeMet : nodeMet);
            }
            return allReqMet;
        }

        /*
         * Adds a new ContractRequirement to handle REQUIREMENT nodes with the given type.
         */
        public static void Register(Type crType, string typeName)
        {
            Debug.Log("ContractConfigurator: Registering ContractRequirement class " +
                crType.FullName + " for handling REQUIREMENT nodes with type = " + typeName + ".");

            if (requirementTypes.ContainsKey(typeName))
            {
                Debug.LogError("Cannot register " + crType.FullName + "[" + crType.Module +
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
                Debug.LogError("ContractConfigurator: No ContractRequirement has been registered for type '" + type + "'.");
                return null;
            }

            // Create an instance of the ContractRequirement
            ContractRequirement requirement = (ContractRequirement)Activator.CreateInstance(requirementTypes[type]);

            // Set attributes
            requirement.contractType = contractType;
            requirement.targetBody = contractType.targetBody;

            // Load config
            if (!requirement.Load(configNode))
            {
                return null;
            }

            return requirement;
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return "REQUIREMENT '" + configNode.GetValue("name") + "' of type '" + configNode.GetValue("type") + "'";
        }
    }
}
