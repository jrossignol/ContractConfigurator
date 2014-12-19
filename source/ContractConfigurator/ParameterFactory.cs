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
     * Class for generating ContractParameter objects.
     */
    public abstract class ParameterFactory : IContractConfiguratorFactory
    {
        private static Dictionary<string, Type> factories = new Dictionary<string, Type>();

        protected virtual ContractType contractType { get; set; }
        protected virtual CelestialBody targetBody { get; set; }
        protected virtual float rewardScience { get; set; }
        protected virtual float rewardReputation { get; set; }
        protected virtual float rewardFunds { get; set; }
        protected virtual float failureReputation { get; set; }
        protected virtual float failureFunds { get; set; }
        protected virtual bool optional { get; set; }
        protected virtual bool? disableOnStateChange { get; set; }
        protected virtual List<ParameterFactory> childNodes { get; set; }
        protected virtual string title { get; set; }

        /*
         * Loads the ParameterFactory from the given ConfigNode.  The base version performs the following:
         *   - Loads and validates the values for
         *       - rewardScience
         *       - rewardReputation
         *       - rewardFunds
         *       - failureReputation
         *       - failureFunds
         *       - advanceFunds
         *       - optional
         *       - targetBody
         *       - disableOnStateChange
         *       - child PARAMETER nodes
         */
        public virtual bool Load(ConfigNode configNode)
        {
            bool valid = true;

            // Load targetBody
            CelestialBody body = ConfigNodeUtil.ParseCelestialBody(configNode, "targetBody");
            if (body != null) {
                targetBody = body;
            }

            // Load rewards
            rewardFunds = (float)Convert.ToDouble(configNode.GetValue("rewardFunds"));
            rewardReputation = (float)Convert.ToDouble(configNode.GetValue("rewardReputation"));
            rewardScience = (float)Convert.ToDouble(configNode.GetValue("rewardScience"));
            failureFunds = (float)Convert.ToDouble(configNode.GetValue("failureFunds"));
            failureReputation = (float)Convert.ToDouble(configNode.GetValue("failureReputation"));

            // Load optional flag
            if (configNode.HasValue("optional"))
            {
                optional = Convert.ToBoolean(configNode.GetValue("optional"));
            }
            else
            {
                optional = false;
            }

            // Load disableOnStateChange flag
            if (configNode.HasValue("disableOnStateChange"))
            {
                disableOnStateChange = Convert.ToBoolean(configNode.GetValue("disableOnStateChange"));
            }
            else
            {
                disableOnStateChange = null;
            }

            // Get title
            title = configNode.HasValue("title") ? configNode.GetValue("title") : null;

            // Load child nodes
            childNodes = new List<ParameterFactory>();
            foreach (ConfigNode childNode in configNode.GetNodes("PARAMETER"))
            {
                ParameterFactory child = ParameterFactory.GenerateParameterFactory(childNode, contractType);
                if (child != null)
                {
                    childNodes.Add(child);
                }
            }

            return valid;
        }

        /*
         * Method for generating ContractParameter objects.  Each time it is called it should
         * generate a new parameter for the given contract.  The parameter does not need to be
         * added to the contract, as that gets done elsewhere (the contract is simply passed
         * to be used in parameter generation logic).  The following members also do not need to
         * be loaded for the ContractParameter (they get handled after this method returns):
         *   - title
         *   - rewardScience
         *   - rewardReputation
         *   - rewardFunds
         *   - failureReputation
         *   - failureFunds
         *   - advanceFunds
         *   - optional
         *   - disableOnStateChange
         *   - child PARAMETER nodes
         */
        public abstract ContractParameter Generate(Contract contract);

        /*
         * Method for generating ContractParameter objects.  This will call the Generate() method
         * on the sub-class, load all common parameters and load child parameters.
         */
        public virtual ContractParameter Generate(Contract contract, IContractParameterHost contractParamHost)
        {
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

            return parameter;
        }

        /*
         * Generates all the ContractParameter objects required for the array of ConfigNodes, and
         * adds them to the host object.
         */
        public static void GenerateParameters(ConfiguredContract contract, IContractParameterHost contractParamHost, List<ParameterFactory> paramFactories)
        {
            foreach (ParameterFactory paramFactory in paramFactories)
            {
                ContractParameter parameter = paramFactory.Generate(contract, contractParamHost);

                // Get the child parameters
                GenerateParameters(contract, parameter, paramFactory.childNodes);
            }
        }

        /*
         * Adds a new ParameterFactory to handle PARAMETER nodes with the given type.
         */
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

        /*
         * Generates a new ParameterFactory from the given ConfigNode.
         */
        public static ParameterFactory GenerateParameterFactory(ConfigNode parameterConfig, ContractType contractType)
        {
            // Get the type
            string type = parameterConfig.GetValue("type");
            if (!factories.ContainsKey(type))
            {
                LoggingUtil.LogError(typeof(ParameterFactory), "No ParameterFactory has been registered for type '" + type + "'.");
                return null;
            }

            // Create an instance of the factory
            ParameterFactory paramFactory = (ParameterFactory)Activator.CreateInstance(factories[type]);

            // Set attributes
            paramFactory.contractType = contractType;
            paramFactory.targetBody = contractType.targetBody;

            // Load config
            if (!paramFactory.Load(parameterConfig))
            {
                return null;
            }

            return paramFactory;
        }

        public string ErrorPrefix(ConfigNode configNode)
        {
            return "PARAMETER '" + configNode.GetValue("name") + "' of type '" + configNode.GetValue("type") + "'";
        }
    }
}
