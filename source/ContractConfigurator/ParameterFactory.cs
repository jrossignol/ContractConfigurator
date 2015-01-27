using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for generating ContractParameter objects.
    /// </summary>
    public abstract class ParameterFactory : IContractConfiguratorFactory
    {
        private static Dictionary<string, Type> factories = new Dictionary<string, Type>();

        protected string name;
        protected string type;
        protected virtual ContractType contractType { get; set; }
        protected CelestialBody targetBody;
        protected float rewardScience;
        protected float rewardReputation;
        protected float rewardFunds;
        protected float failureReputation;
        protected float failureFunds;
        protected bool optional;
        protected bool? disableOnStateChange;
        protected ParameterFactory parent = null;
        protected virtual List<ParameterFactory> childNodes { get; set; }
        protected virtual List<ContractRequirement> requirements { get; set; }
        protected string title;
        public string log;

        public bool enabled = true;
        public virtual IEnumerable<ParameterFactory> ChildParameters { get { return childNodes; } }
        public virtual IEnumerable<ContractRequirement> ChildRequirements { get { return requirements; } }
        public string config = "";

        /// <summary>
        /// Loads the ParameterFactory from the given ConfigNode.  The base version performs the following:
        ///   - Loads and validates the values for
        ///     - rewardScience
        ///     - rewardReputation
        ///     - rewardFunds
        ///     - failureReputation
        ///     - failureFunds
        ///     - advanceFunds
        ///     - optional
        ///     - targetBody
        ///     - disableOnStateChange
        ///     - child PARAMETER nodes
        /// </summary>
        /// <param name="configNode">The ConfigNode to load</param>
        /// <returns>Whether the load was successful</returns>
        public virtual bool Load(ConfigNode configNode)
        {
            bool valid = true;

            // Get name and type
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "type", ref type, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", ref name, this, type);

            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", ref targetBody, this, contractType.targetBody);

            // Load rewards
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardFunds", ref rewardFunds, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardReputation", ref rewardReputation, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "rewardScience", ref rewardScience, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureFunds", ref failureFunds, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "failureReputation", ref failureReputation, this, 0.0f, x => Validation.GE(x, 0.0f));

            // Load flags
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "optional", ref optional, this, false);
            valid &= ConfigNodeUtil.ParseValue<bool?>(configNode, "disableOnStateChange", ref disableOnStateChange, this, (bool?)null);

            // Get title
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", ref title, this, (string)null);

            // Load child nodes
            childNodes = new List<ParameterFactory>();
            foreach (ConfigNode childNode in configNode.GetNodes("PARAMETER"))
            {
                ParameterFactory child = null;
                valid &= ParameterFactory.GenerateParameterFactory(childNode, contractType, out child, this);
                if (child != null)
                {
                    childNodes.Add(child);
                }
            }

            // Load child requirements
            requirements = new List<ContractRequirement>();
            foreach (ConfigNode childNode in configNode.GetNodes("REQUIREMENT"))
            {
                ContractRequirement req = null;
                valid &= ContractRequirement.GenerateRequirement(childNode, contractType, out req);
                if (req != null)
                {
                    requirements.Add(req);
                }
            }

            config = configNode.ToString();
            return valid;
        }

        /// <summary>
        /// Method for generating ContractParameter objects.  Each time it is called it should
        /// generate a new parameter for the given contract.  The parameter does not need to be
        /// added to the contract, as that gets done elsewhere (the contract is simply passed
        /// to be used in parameter generation logic).  The following members also do not need to
        /// be loaded for the ContractParameter (they get handled after this method returns):
        ///   - title
        ///   - rewardScience
        ///   - rewardReputation
        ///   - rewardFunds
        ///   - failureReputation
        ///   - failureFunds
        ///   - advanceFunds
        ///   - optional
        ///   - disableOnStateChange
        ///   - child PARAMETER nodes
        /// </summary>
        /// <param name="contract">Contract to generate for</param>
        /// <returns>The created ContractParameter</returns>
        public abstract ContractParameter Generate(Contract contract);

        /// <summary>
        /// Method for generating ContractParameter objects.  This will call the Generate() method 
        /// on the sub-class, load all common parameters and load child parameters.
        /// </summary>
        /// <param name="contract">Contract to generate for</param>
        /// <param name="contractParamHost">Parent object for the ContractParameter</param>
        /// <returns>Generated ContractParameter</returns>
        public virtual ContractParameter Generate(ConfiguredContract contract, IContractParameterHost contractParamHost)
        {
            // First check any requirements
            if (!ContractRequirement.RequirementsMet(contract, contract.contractType, requirements))
            {
                return null;
            }

            // Generate a parameter using the sub-class logic
            ContractParameter parameter = Generate(contract);
            if (parameter == null)
            {
                throw new Exception(GetType().FullName + ".Generate() returned a null ContractParameter!");
            }

            // Add ContractParameter to the host
            contractParamHost.AddParameter(parameter);

            // Set the funds/science/reputation parameters
            parameter.SetFunds(rewardFunds, failureFunds, targetBody);
            parameter.SetReputation(rewardReputation, failureReputation, targetBody);
            parameter.SetScience(rewardScience, targetBody);

            // Set other flags
            parameter.Optional = optional;
            if (disableOnStateChange != null)
            {
                parameter.DisableOnStateChange = (bool)disableOnStateChange;
            }
            parameter.ID = name;

            return parameter;
        }

        /// <summary>
        /// Generates all the ContractParameter objects required for the array of ConfigNodes, and 
        /// adds them to the host object.
        /// </summary>
        /// <param name="contract">Contract to generate for</param>
        /// <param name="contractParamHost">The object to use as a parent for ContractParameters</param>
        /// <param name="paramFactories">The ParameterFactory objects to use to generate parameters.</param>
        public static void GenerateParameters(ConfiguredContract contract, IContractParameterHost contractParamHost, List<ParameterFactory> paramFactories)
        {
            foreach (ParameterFactory paramFactory in paramFactories)
            {
                if (paramFactory.enabled)
                {
                    ContractParameter parameter = paramFactory.Generate(contract, contractParamHost);

                    // Get the child parameters
                    if (parameter != null)
                    {
                        GenerateParameters(contract, parameter, paramFactory.childNodes);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new ParameterFactory to handle PARAMETER nodes with the given type.
        /// </summary>
        /// <param name="factoryType">Type of factory to register.</param>
        /// <param name="typeName">The name of the factory.</param>
        public static void Register(Type factoryType, string typeName)
        {
            LoggingUtil.LogDebug(typeof(ParameterFactory), "Registering parameter factory class " +
                factoryType.FullName + " for handling PARAMETER nodes with type = " + typeName + ".");

            if (factories.ContainsKey(typeName))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "Cannot register " + factoryType.FullName + "[" + factoryType.Module +
                    "] to handle type " + typeName + ": already handled by " +
                    factories[typeName].FullName + "[" +
                    factories[typeName].Module + "]");
            }
            else
            {
                factories.Add(typeName, factoryType);
            }
        }


        /// <summary>
        /// Generates a new ParameterFactory from the given ConfigNode.
        /// </summary>
        /// <param name="parameterConfig">ConfigNode to use in the generation.</param>
        /// <param name="contractType">ContractType that this parameter factory falls under</param>
        /// <param name="paramFactory">The ParameterFactory object.</param>
        /// <param name="parent">ParameterFactory to use as the parent</param>
        /// <returns>Whether the load was successful</returns>
        public static bool GenerateParameterFactory(ConfigNode parameterConfig, ContractType contractType, out ParameterFactory paramFactory, ParameterFactory parent = null)
        {
            // Logging on
            LoggingUtil.CaptureLog = true;

            // Get the type
            string type = parameterConfig.GetValue("type");
            if (!factories.ContainsKey(type))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "CONTRACT_TYPE '" + contractType.name + "'," +
                    "PARAMETER '" + parameterConfig.GetValue("name") + "' of type '" + parameterConfig.GetValue("type") + "': " +
                    "No ParameterFactory has been registered for type '" + type + "'.");
                paramFactory = new InvalidParameterFactory();
            }
            else
            {
                // Create an instance of the factory
                paramFactory = (ParameterFactory)Activator.CreateInstance(factories[type]);
            }

            // Set attributes
            paramFactory.parent = parent;
            paramFactory.contractType = contractType;

            // Load config
            bool valid = paramFactory.Load(parameterConfig);

            // Check for unexpected values - always do this last
            if (paramFactory.GetType() != typeof(InvalidParameterFactory))
            {
                valid &= ConfigNodeUtil.ValidateUnexpectedValues(parameterConfig, paramFactory);
            }

            paramFactory.enabled = valid;
            paramFactory.log = LoggingUtil.capturedLog;
            LoggingUtil.CaptureLog = false;

            return valid;
        }

        /// <summary>
        /// Standard prefix used in error messages.
        /// </summary>
        /// <returns>Prefix for error messages.</returns>
        public string ErrorPrefix()
        {
            return (contractType != null ? "CONTRACT_TYPE '" + contractType.name + "', " : "") + 
                "PARAMETER '" + name + "' of type '" + type + "'";
        }

        /// <summary>
        /// Standard prefix used in error messages.
        /// </summary>
        /// <param name="configNode">The ConfigNode to draw details from.</param>
        /// <returns>Prefix for error messages.</returns>
        public string ErrorPrefix(ConfigNode configNode)
        {
            return (contractType != null ? "CONTRACT_TYPE '" + contractType.name + "', " : "") + 
                "PARAMETER '" + configNode.GetValue("name") + "' of type '" + configNode.GetValue("type") + "'";
        }

        /// <summary>
        /// Validates whether the targetBody value has been loaded. 
        /// </summary>
        /// <param name="configNode">The ConfigNode to validate against</param>
        /// <returns>True if the targetBody has been loaded, logs and error and returns false otherwise.</returns>
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

        /// <summary>
        /// Gets the identifier for the parameter.
        /// </summary>
        /// <returns>String for the parameter.</returns>
        public override string ToString()
        {
            return "PARAMETER [" + type + "]" + (name != type ? ", (" + name + ")" : "");
        }
    }
}
