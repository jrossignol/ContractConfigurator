using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Behaviour;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for generating ContractBehaviour objects.
    /// </summary>
    public abstract class BehaviourFactory : IContractConfiguratorFactory
    {
        private static Dictionary<string, Type> factories = new Dictionary<string, Type>();

        protected string name;
        protected string type;

        protected virtual ContractType contractType { get; set; }
        protected CelestialBody _targetBody = null;
        protected CelestialBody targetBody
        {
            get { return _targetBody ?? contractType.targetBody; }
        }

        public bool enabled = true;
        public bool hasWarnings { get; set; }
        public Type iteratorType { get; set; }
        public string iteratorKey { get; set; }
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

        /// <summary>
        /// Loads the BehaviourFactory from the given ConfigNode.
        /// </summary>
        /// <param name="configNode">ConfigNode to load from</param>
        /// <returns>Whether the load was successful</returns>
        public virtual bool Load(ConfigNode configNode)
        {
            bool valid = true;
            ConfigNodeUtil.SetCurrentDataNode(dataNode);

            // Get name and type
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "type", x => type = x, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", x => name = x, this, type);

            // Load targetBody
            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", x => _targetBody = x, this, (CelestialBody)null);

            config = configNode.ToString();
            return valid;
        }

        /// <summary>
        /// Method for generating ContractBehaviour objects.  Each time it is called it should
        /// generate a new object for the given contract.  The object does not need to be
        /// added to the contract, as that gets done elsewhere (the contract is simply passed
        /// to be used in behaviour generation logic).
        /// </summary>
        /// <param name="contract">Contract to add behaviour to</param>
        /// <returns>The behaviour object</returns>
        public abstract ContractBehaviour Generate(ConfiguredContract contract);

        /// <summary>
        /// Generates all the ContractBehaviour objects required for the array of ConfigNodes, and
        /// adds them to the host object.
        /// </summary>
        /// <param name="contract">Contract to generate behaviours for</param>
        /// <param name="behaviourNodes">The behaviour factories to use</param>
        /// <return>Whether generation was successful or not</return>
        public static bool GenerateBehaviours(ConfiguredContract contract, List<BehaviourFactory> behaviourNodes)
        {
            foreach (BehaviourFactory behaviourFactory in behaviourNodes)
            {
                if (behaviourFactory.enabled)
                {
                    ContractBehaviour behaviour = behaviourFactory.Generate(contract);
                    if (behaviour == null)
                    {
                        throw new Exception(behaviourFactory.GetType().FullName + ".Generate() returned a null ContractBehaviour!");
                    }

                    // Add ContractBehaviour to the host
                    contract.AddBehaviour(behaviour);
                }
            }

            return true;
        }

        /// <summary>
        /// Adds a new BehaviourFactory to handle Behaviour nodes with the given type.
        /// </summary>
        /// <param name="factoryType">Type of the factory</param>
        /// <param name="typeName">Name to associate with the given type</param>
        public static void Register(Type factoryType, string typeName)
        {
            LoggingUtil.LogDebug(typeof(BehaviourFactory), "Registering behaviour factory class {0} for handling BEHAVIOUR nodes with type = {1}.", factoryType.FullName, typeName);

            if (factories.ContainsKey(typeName))
            {
                LoggingUtil.LogError(typeof(BehaviourFactory), "Cannot register {0}[{1}] to handle type {2}: already handled by {3}[{4}]",
                    factoryType.FullName, factoryType.Module.ToString(), typeName, factories[typeName].FullName, factories[typeName].Module.ToString());
            }
            else
            {
                // Make sure we can instantiate it (this will also run any static initializers)
                Activator.CreateInstance(factoryType);

                // Add it to our list
                factories.Add(typeName, factoryType);
            }
        }

        /// <summary>
        /// Generates a BehaviourFactory from a configuration node.
        /// </summary>
        /// <param name="behaviourConfig">ConfigNode to use in the generation.</param>
        /// <param name="contractType">ContractType that this behaviour falls under</param>
        /// <param name="behaviourFactory">The BehaviourFactory object.</param>
        /// <returns>Whether the load was successful</returns>
        public static bool GenerateBehaviourFactory(ConfigNode behaviourConfig, ContractType contractType, out BehaviourFactory behaviourFactory)
        {
            // Logging on
            LoggingUtil.CaptureLog = true;
            bool valid = true;

            // Get the type
            string type = behaviourConfig.GetValue("type");
            string name = behaviourConfig.HasValue("name") ? behaviourConfig.GetValue("name") : type;
            if (string.IsNullOrEmpty(type))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "CONTRACT_TYPE '{0}', BEHAVIOUR '{1}' does not specify the mandatory 'type' attribute.",
                    contractType.name, behaviourConfig.GetValue("name"));
                behaviourFactory = new InvalidBehaviourFactory();
                valid = false;
            }
            else if (!factories.ContainsKey(type))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "CONTRACT_TYPE '{0}', BEHAVIOUR '{1}' of type '{2}': Unknown behaviour '{3}'.",
                    contractType.name, behaviourConfig.GetValue("name"), behaviourConfig.GetValue("type"), type);
                behaviourFactory = new InvalidBehaviourFactory();
                valid = false;
            }
            else
            {
                // Create an instance of the factory
                behaviourFactory = (BehaviourFactory)Activator.CreateInstance(factories[type]);
            }

            // Set attributes
            behaviourFactory.contractType = contractType;
            behaviourFactory.dataNode = new DataNode(name, contractType.dataNode, behaviourFactory);

            // Load config
            valid &= behaviourFactory.Load(behaviourConfig);

            // Check for unexpected values - always do this last
            if (behaviourFactory.GetType() != typeof(InvalidBehaviourFactory))
            {
                valid &= ConfigNodeUtil.ValidateUnexpectedValues(behaviourConfig, behaviourFactory);
            }

            behaviourFactory.enabled = valid;
            behaviourFactory.log = LoggingUtil.capturedLog;
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
                return StringBuilderCache.Format("CONTRACT_TYPE '{0}', BEHAVIOUR '{1}' of type '{2}'", contractType.name, (name ?? "<blank>"), type);
            }
            else
            {
                return StringBuilderCache.Format("BEHAVIOUR '{1}' of type '{2}'", (name ?? "<blank>"), type);
            }
        }

        /// <summary>
        /// Gets the identifier for the parameter.
        /// </summary>
        /// <returns>String for the parameter.</returns>
        public override string ToString()
        {
            return "BEHAVIOUR [" + type + "]" + (name != type ? ", (" + name + ")" : "");
        }
    }
}
