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

        protected string title;
        protected bool needsTitle = false;
        public bool hideChildren;

        public bool enabled = true;
        public bool hasWarnings { get; set; }
        public Type iteratorType { get; set; }
        public string iteratorKey { get; set; }
        public bool? lastResult = null;
        public virtual IEnumerable<ContractRequirement> ChildRequirements { get { return childNodes; } }
        public string config { get; private set; }
        public string log { get; private set; }
        public DataNode dataNode { get; private set; }
        public Version minVersion
        {
            get
            {
                return contractType.minVersion;
            }
        }

        protected bool allowKerbin = true;

        /// <summary>
        /// Loads the ContractRequirement from the given ConfigNode.  The base version loads the following:
        ///     - child nodes
        ///     - invertRequirement
        /// </summary>
        /// <param name="configNode">Config node to load from</param>
        /// <returns>Whether the load was successful or not.</returns>
        public virtual bool LoadFromConfig(ConfigNode configNode)
        {
            bool valid = true;
            ConfigNodeUtil.SetCurrentDataNode(dataNode);

            // Get name and type
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "type", x => type = x, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", x => name = x, this, type);

            // Override needsTitle if a child of a parameter
            if (needsTitle && dataNode.Parent.Factory is ParameterFactory)
            {
                needsTitle = false;
            }

            // Allow reading of a custom title
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", x => title = x, this, (string)null);

            // Whether to hide child requirements in the mission control display
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hideChildren", x => hideChildren = x, this, false);

            if (!configNode.HasValue("targetBody"))
            {
                configNode.AddValue("targetBody", "@/targetBody");
            }
            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", x => _targetBody = x, this, (CelestialBody)null,
                x => allowKerbin || Validation.NE(x.name, FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).FirstOrDefault().name));

            // By default, do not check the requirement for active contracts
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "checkOnActiveContract", x => checkOnActiveContract = x, this, false);

            // Load invertRequirement flag
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement", x => invertRequirement = x, this, false);

            config = configNode.ToString();
            return valid;
        }

        public void Save(ConfigNode configNode)
        {
            // Special case - don't save disabled requirements
            if (!enabled)
            {
                return;
            }

            configNode.AddValue("name", name);
            configNode.AddValue("type", type);
            if (_targetBody != null)
            {
                configNode.AddValue("targetBody", _targetBody.name);
            }
            configNode.AddValue("invertRequirement", invertRequirement);
            configNode.AddValue("checkOnActiveContract", checkOnActiveContract);

            foreach (ContractRequirement requirement in childNodes)
            {
                ConfigNode child = new ConfigNode("REQUIREMENT");
                configNode.AddNode(child);
                requirement.Save(child);
            }

            OnSave(configNode);
        }

        public abstract void OnSave(ConfigNode configNode);

        public void Load(ConfigNode configNode)
        {
            name = ConfigNodeUtil.ParseValue<string>(configNode, "name");
            type = ConfigNodeUtil.ParseValue<string>(configNode, "type");
            _targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", (CelestialBody)null);
            invertRequirement = ConfigNodeUtil.ParseValue<bool>(configNode, "invertRequirement");
            checkOnActiveContract = ConfigNodeUtil.ParseValue<bool>(configNode, "checkOnActiveContract");

            OnLoad(configNode);
        }

        public abstract void OnLoad(ConfigNode configNode);

        /// <summary>
        /// Loads a requirement from a ConfigNode.
        /// </summary>
        /// <param name="configNode"></param>
        /// <param name="contract"></param>
        /// <returns></returns>
        public static ContractRequirement LoadRequirement(ConfigNode configNode)
        {
            // Determine the type
            string typeName = configNode.GetValue("type");
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }
            Type type = requirementTypes.ContainsKey(typeName) ? requirementTypes[typeName] : null;
            if (type == null)
            {
                throw new Exception("No ContractRequirement with type = '" + typeName + "'.");
            }

            // Instantiate and load
            ContractRequirement requirement = (ContractRequirement)Activator.CreateInstance(type);
            requirement.Load(configNode);

            // Load children
            foreach (ConfigNode child in configNode.GetNodes("REQUIREMENT"))
            {
                ContractRequirement childRequirement = LoadRequirement(child);
                if (childRequirement != null)
                {
                    requirement.childNodes.Add(childRequirement);
                }
            }

            return requirement;
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

        protected abstract string RequirementText();
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(title))
                {
                    return RequirementText();
                }
                else
                {
                    return title;
                }
            }
        }

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
            LoggingUtil.LogVerbose(typeof(ContractRequirement), "Checked requirement '{0}' of type {1}: {2}", name, type, nodeMet);
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
                LoggingUtil.LogVerbose(typeof(ContractRequirement), "Checking requirements for contract '{0}'", contractType.name);
                foreach (ContractRequirement requirement in contractRequirements)
                {
                    if (requirement.enabled)
                    {
                        if (requirement.checkOnActiveContract || contract == null  || contract.ContractState != Contract.State.Active)
                        {
                            allReqMet = allReqMet && requirement.CheckRequirement(contract);

                            if (!allReqMet)
                            {
                                LoggingUtil.Log(contract != null && contract.ContractState == Contract.State.Active ? LoggingUtil.LogLevel.INFO :
                                    contract != null && contract.ContractState == Contract.State.Offered ? LoggingUtil.LogLevel.DEBUG : LoggingUtil.LogLevel.VERBOSE,
                                    requirement.GetType(), "Contract {0}: requirement {1} was not met.", contractType.name, requirement.name);
                                break;
                            }
                        }
                    }
                }

                // Force fail the contract if a requirement becomes unmet
                if (contract != null && contract.ContractState == Contract.State.Active && !allReqMet)
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
            LoggingUtil.LogDebug(typeof(ContractRequirement), "Registering ContractRequirement class {0} for handling REQUIREMENT nodes with type = {1}.", crType.FullName, typeName);

            if (requirementTypes.ContainsKey(typeName))
            {
                LoggingUtil.LogError(typeof(ContractRequirement), "Cannot register {0}[{1}] to handle type {2}: already handled by {3}[{4}]",
                    crType.FullName, crType.Module.ToString(), typeName, requirementTypes[typeName].FullName, requirementTypes[typeName].Module.ToString());
            }
            else
            {
                // Make sure we can instantiate it (this will also run any static initializers)
                Activator.CreateInstance(crType);

                // Add it to our list
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
                LoggingUtil.LogError(typeof(ParameterFactory), "CONTRACT_TYPE '{0}', REQUIREMENT '{1}' does not specify the mandatory 'type' attribute.",
                    contractType.name, configNode.GetValue("name"));
                requirement = new InvalidContractRequirement();
                valid = false;
            }
            else if (!requirementTypes.ContainsKey(type))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "CONTRACT_TYPE '{0}', REQUIREMENT '{1}' of type '{2}': Unknown requirement '{3}'.",
                    contractType.name, configNode.GetValue("name"), configNode.GetValue("type"), type);
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
            valid &= requirement.LoadFromConfig(configNode);

            // Override the needsTitle if we have a parent node with hideChildren
            ContractRequirement parentRequirement = parent as ContractRequirement;
            if (parentRequirement != null)
            {
                if (parentRequirement.hideChildren)
                {
                    requirement.hideChildren = true;
                    requirement.needsTitle = false;
                }
            }

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

            // Error for missing title
            if (requirement.needsTitle && string.IsNullOrEmpty(requirement.title))
            {
                valid = contractType.minVersion < ContractConfigurator.ENHANCED_UI_VERSION;
                LoggingUtil.Log(contractType.minVersion >= ContractConfigurator.ENHANCED_UI_VERSION ? LoggingUtil.LogLevel.ERROR : LoggingUtil.LogLevel.WARNING,
                    requirement, "{0}: missing required attribute 'title'.", requirement.ErrorPrefix(configNode));
            }

            requirement.enabled = valid;
            requirement.log = LoggingUtil.capturedLog;
            LoggingUtil.CaptureLog = false;

            return valid;
        }


        public string ErrorPrefix()
        {
            return ErrorPrefix(name, type);
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return ErrorPrefix(configNode.GetValue("name"), type ?? configNode.GetValue("type"));
        }

        private string ErrorPrefix(string name, string type)
        {
            if (contractType != null)
            {
                return StringBuilderCache.Format("CONTRACT_TYPE '{0}', REQUIREMENT '{1}' of type '{2}'", contractType.name, (name ?? "<blank>"), type);
            }
            else
            {
                return StringBuilderCache.Format("REQUIREMENT '{1}' of type '{2}'", (name ?? "<blank>"), type);
            }
        }

        /// <summary>
        /// Validates whether the targetBody value has been loaded.
        /// </summary>
        /// <param name="configNode">ConfigNode to check</param>
        /// <returns>True if the value is loaded, logs and error and returns false otherwise.</returns>
        protected virtual bool ValidateTargetBody(ConfigNode configNode)
        {
            if (targetBody == null && dataNode.IsDeterministic("targetBody") && dataNode.IsInitialized("targetBody"))
            {
                LoggingUtil.LogError(this, "{0}: targetBody for {1} must be specified.", ErrorPrefix(configNode), GetType());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates whether the targetBody value has been loaded. 
        /// </summary>
        /// <returns>True if the targetBody has been loaded, false otherwise.</returns>
        protected virtual bool ValidateTargetBody()
        {
            return targetBody != null;
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
