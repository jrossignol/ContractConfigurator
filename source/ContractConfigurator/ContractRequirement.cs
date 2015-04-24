using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for capturing a requirement for making a contract available.
    /// </summary>
    public abstract class ContractRequirement : IContractConfiguratorFactory
    {
        private static Dictionary<string, Type> requirementTypes = new Dictionary<string, Type>();

        public string Name { get { return name; } }
        public string Type { get { return type; } }
        protected string name;
        protected string type;

        public bool InvertRequirement { get { return invertRequirement; } }
        protected List<ContractRequirement> childNodes = new List<ContractRequirement>();
        protected virtual ContractType contractType { get; set; }
        protected CelestialBody _targetBody = null;
        protected CelestialBody targetBody
        {
            get { return _targetBody ?? contractType.targetBody; }
        }
        public bool invertRequirement;
        protected bool checkOnActiveContract;

        public bool enabled = true;
        public bool hasWarnings { get; set; }
        public bool? lastResult = null;
        public virtual IEnumerable<ContractRequirement> ChildRequirements { get { return childNodes; } }
        public string config { get; private set; }
        public string log { get; private set; }
        public DataNode dataNode { get; private set; }

        /// <summary>
        /// Loads the ContractRequirement from the given ConfigNode.  The base version loads the following:
        ///     - child nodes
        ///     - invertRequirement
        /// </summary>
        /// <param name="configNode">Config node to load from</param>
        /// <returns>Whether the load was successful or not.</returns>
        public virtual bool Load(ConfigNode configNode)
        {
            bool valid = true;
            ConfigNodeUtil.SetCurrentDataNode(dataNode);

            // Get name and type
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "type", x => type = x, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", x => name = x, this, type);

            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", x => _targetBody = x, this, (CelestialBody)null);

            // By default, do not check the requirement for active contracts
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "checkOnActiveContract", x => checkOnActiveContract = x, this, false);

            // Load invertRequirement flag
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false);

            config = configNode.ToString();
            return valid;
        }

        /// <summary>
        /// Method for checking whether a contract meets the requirement to be offered.  When called
        /// it should check whether the requirement is met.  The passed contract can be used as part
        /// of the validation.
        /// 
        /// If child requirements are supported, then the class implementing this method is
        /// responsible for checking those requirements.
        /// </summary>
        /// <param name="contract">Contract to check</param>
        /// <returns>Whether the requirement is met for the given contract.</returns>
        public virtual bool RequirementMet(ConfiguredContract contract) { return true; }

        /// <summary>
        /// Checks the requirement for the given contract.
        /// </summary>
        /// <param name="contract">Contract to check</param>
        /// <returns>Whether the requirement is met</returns>
        public virtual bool CheckRequirement(ConfiguredContract contract)
        {
            bool nodeMet = RequirementMet(contract);
            nodeMet = invertRequirement ? !nodeMet : nodeMet;
            lastResult = nodeMet;
            LoggingUtil.LogVerbose(typeof(ContractRequirement), "Checked requirement '" + name + "' of type " + type + ": " + nodeMet);
            return nodeMet;
        }

        /// <summary>
        /// Checks if all the given ContractRequirement meet the requirement.
        /// </summary>
        /// <param name="contract">Contract to check</param>
        /// <param name="contractType">Contract type of the contract (in case the contract type has not yet been assigned).</param>
        /// <param name="contractRequirements">The list of requirements to check</param>
        /// <returns>Whether the requirement is met or not.</returns>
        public static bool RequirementsMet(ConfiguredContract contract, ContractType contractType, IEnumerable<ContractRequirement> contractRequirements)
        {
            bool allReqMet = true;
            try
            {
                LoggingUtil.LogVerbose(typeof(ContractRequirement), "Checking requirements for contract '" + contractType.name);
                foreach (ContractRequirement requirement in contractRequirements)
                {
                    if (requirement.enabled)
                    {
                        if (requirement.checkOnActiveContract || contract.ContractState != Contract.State.Active)
                        {
                            allReqMet = allReqMet && requirement.CheckRequirement(contract);

                            if (!allReqMet)
                            {
                                LoggingUtil.Log(contract.ContractState == Contract.State.Active ? LoggingUtil.LogLevel.DEBUG : LoggingUtil.LogLevel.VERBOSE,
                                    requirement.GetType(), "Requirement " + requirement.name + " was not met.");
                                break;
                            }
                        }
                    }
                }

                // Force fail the contract if a requirement becomes unmet
                if (contract.ContractState == Contract.State.Active && !allReqMet)
                {
                    // Fail the contract - unfortunately, the player won't know why. :(
                    contract.Fail();

                    // Force the stock contracts window to refresh
                    GameEvents.Contract.onContractsLoaded.Fire();
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogException(new Exception("ContractConfigurator: Exception checking requirements!", e));
                return false;
            }
            return allReqMet;
        }

        /// <summary>
        /// Adds a new ContractRequirement to handle REQUIREMENT nodes with the given type.
        /// </summary>
        /// <param name="crType">ContractRequirement type</param>
        /// <param name="typeName">Name to associate to the type</param>
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

        /// <summary>
        /// Generates a ContractRequirement from a configuration node.
        /// </summary>
        /// <param name="configNode">ConfigNode to use in the generation.</param>
        /// <param name="contractType">ContractType that this requirement falls under</param>
        /// <param name="requirement">The ContractRequirement object.</param>
        /// <returns>Whether the load was successful</returns>
        public static bool GenerateRequirement(ConfigNode configNode, ContractType contractType, out ContractRequirement requirement,
            IContractConfiguratorFactory parent = null)
        {
            // Logging on
            LoggingUtil.CaptureLog = true;
            bool valid = true;

            // Get the type
            string type = configNode.GetValue("type");
            string name = configNode.HasValue("name") ? configNode.GetValue("name") : type;
            if (string.IsNullOrEmpty(type))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "CONTRACT_TYPE '" + contractType.name + "'," +
                    "REQUIREMENT '" + configNode.GetValue("name") + "' does not specify the mandatory 'type' attribute.");
                requirement = new InvalidContractRequirement();
                valid = false;
            }
            else if (!requirementTypes.ContainsKey(type))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "CONTRACT_TYPE '" + contractType.name + "'," +
                    "REQUIREMENT '" + configNode.GetValue("name") + "' of type '" + configNode.GetValue("type") + "': " +
                    "No ContractRequirement has been registered for type '" + type + "'.");
                requirement = new InvalidContractRequirement();
                valid = false;
            }
            else
            {
                // Create an instance of the ContractRequirement
                requirement = (ContractRequirement)Activator.CreateInstance(requirementTypes[type]);
            }

            // Set attributes
            requirement.contractType = contractType;
            requirement.dataNode = new DataNode(name, parent != null ? parent.dataNode : contractType.dataNode, requirement);

            // Load config
            valid &= requirement.Load(configNode);

            // Check for unexpected values - always do this last
            if (requirement.GetType() != typeof(InvalidContractRequirement))
            {
                valid &= ConfigNodeUtil.ValidateUnexpectedValues(configNode, requirement);
            }

            // Load child nodes
            foreach (ConfigNode childNode in ConfigNodeUtil.GetChildNodes(configNode, "REQUIREMENT"))
            {
                ContractRequirement child = null;
                valid &= ContractRequirement.GenerateRequirement(childNode, contractType, out child, requirement);
                if (child != null)
                {
                    requirement.childNodes.Add(child);
                    if (child.hasWarnings)
                    {
                        requirement.hasWarnings = true;
                    }
                }
            }

            requirement.enabled = valid;
            requirement.log = LoggingUtil.capturedLog;
            LoggingUtil.CaptureLog = false;

            return valid;
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

        /// <summary>
        /// Validates whether the targetBody value has been loaded.
        /// </summary>
        /// <param name="configNode">ConfigNode to check</param>
        /// <returns>True if the value is loaded, logs and error and returns false otherwise.</returns>
        protected virtual bool ValidateTargetBody(ConfigNode configNode)
        {
            if (targetBody == null)
            {
                LoggingUtil.LogError(this, ErrorPrefix(configNode) +
                    ": targetBody for " + GetType() + " must be specified.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the identifier for the parameter.
        /// </summary>
        /// <returns>String for the parameter.</returns>
        public override string ToString()
        {
            return "REQUIREMENT [" + type + "]" + (name != type ? ", (" + name + ")" : "");
        }
    }
}
