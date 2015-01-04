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
     * Class for generating ContractBehaviour objects.
     */
    public abstract class BehaviourFactory : IContractConfiguratorFactory
    {
        private static Dictionary<string, Type> factories = new Dictionary<string, Type>();

        protected string name;
        protected string type;

        protected virtual ContractType contractType { get; set; }
        protected CelestialBody targetBody;

        /*
         * Loads the BehaviourFactory from the given ConfigNode.
         */
        public virtual bool Load(ConfigNode configNode)
        {
            bool valid = true;

            // Get name and type
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "name", ref name, this, "unknown");
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "type", ref type, this);

            // Load targetBody
            valid &= ConfigNodeUtil.ParseValue<CelestialBody>(configNode, "targetBody", ref targetBody, this, (CelestialBody)null);

            return valid;
        }

        /*
         * Method for generating ContractBehaviour objects.  Each time it is called it should
         * generate a new object for the given contract.  The object does not need to be
         * added to the contract, as that gets done elsewhere (the contract is simply passed
         * to be used in behaviour generation logic).
         */
        public abstract ContractBehaviour Generate(ConfiguredContract contract);

        /*
         * Generates all the ContractBehaviour objects required for the array of ConfigNodes, and
         * adds them to the host object.
         */
        public static void GenerateBehaviours(ConfiguredContract contract, List<BehaviourFactory> behaviourNodes)
        {
            foreach (BehaviourFactory behaviourFactory in behaviourNodes)
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

        /*
         * Adds a new BehaviourFactory to handle Behaviour nodes with the given type.
         */
        public static void Register(Type factoryType, string typeName)
        {
            LoggingUtil.LogDebug(typeof(BehaviourFactory), "Registering behaviour factory class " +
                factoryType.FullName + " for handling BEHAVIOUR nodes with type = " + typeName + ".");

            if (factories.ContainsKey(typeName))
            {
                LoggingUtil.LogError(typeof(BehaviourFactory), "Cannot register " + factoryType.FullName + "[" + factoryType.Module +
                    "] to handle type " + typeName + ": already handled by " +
                    factories[typeName].FullName + "[" +
                    factories[typeName].Module + "]");
            }
            else
            {
                factories.Add(typeName, factoryType);
            }
        }

        /*
         * Generates a new BehaviourFactory from the given ConfigNode.
         */
        public static BehaviourFactory GenerateBehaviourFactory(ConfigNode behaviourConfig, ContractType contractType)
        {
            // Get the type
            string type = behaviourConfig.GetValue("type");
            if (!factories.ContainsKey(type))
            {
                LoggingUtil.LogError(typeof(BehaviourFactory), "No BehaviourFactory has been registered for type '" + type + "'.");
                return null;
            }

            // Create an instance of the factory
            BehaviourFactory behaviourFactory = (BehaviourFactory)Activator.CreateInstance(factories[type]);

            // Set attributes
            behaviourFactory.contractType = contractType;
            behaviourFactory.targetBody = contractType.targetBody;

            // Load config
            bool valid = behaviourFactory.Load(behaviourConfig);

            // Check for unexpected values - always do this last
            valid &= ConfigNodeUtil.ValidateUnexpectedValues(behaviourConfig, behaviourFactory);

            return valid ? behaviourFactory : null;
        }

        public string ErrorPrefix()
        {
            return (contractType != null ? "CONTRACT_TYPE '" + contractType.name + "', " : "") + 
                "BEHAVIOUR '" + name + "' of type '" + type + "'";
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return (contractType != null ? "CONTRACT_TYPE '" + contractType.name + "', " : "") + 
                "BEHAVIOUR '" + configNode.GetValue("name") + "' of type '" + configNode.GetValue("type") + "'";
        }
    }
}
