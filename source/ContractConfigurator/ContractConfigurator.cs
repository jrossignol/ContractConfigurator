using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class ContractConfigurator : MonoBehaviour
    {
        static bool loaded = false;
        static bool contractTypesAdjusted = false;

        void Start()
        {
            DontDestroyOnLoad(this);
        }

        void Update()
        {
            // Load all the contract configurator configuration
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !loaded)
            {
                LoggingUtil.LoadDebuggingConfig();
                RegisterParameterFactories();
                RegisterBehaviourFactories();
                RegisterContractRequirements();
                LoadContractConfig();
                loaded = true;
            }
            // Try to disable the contract types
            else if ((HighLogic.LoadedScene == GameScenes.SPACECENTER) && !contractTypesAdjusted)
            {
                if (AdjustContractTypes())
                {
                    contractTypesAdjusted = true;
                }
            }

            // We're done, don't need to keep calling us
            if (loaded && contractTypesAdjusted)
            {
                Destroy(this);
            }
        }

        /*
         * Registers all the out of the box ParameterFactory classes.
         */
        void RegisterParameterFactories()
        {
            LoggingUtil.LogDebug(this.GetType(), "Start Registering ParameterFactories");

            // Get everything that extends ParameterFactory
            var subclasses =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.IsSubclassOf(typeof(ParameterFactory))
                select type;

            // Register each type with the parameter factory
            foreach (Type subclass in subclasses)
            {
                string name = subclass.Name;
                if (name.EndsWith("Factory"))
                {
                    name = name.Remove(name.Length - 7, 7);
                }

                ParameterFactory.Register(subclass, name);
            }

            LoggingUtil.LogInfo(this.GetType(), "Finsished Registering ParameterFactories");
        }

        /*
         * Registers all the out of the box BehaviourFactory classes.
         */
        void RegisterBehaviourFactories()
        {
            LoggingUtil.LogDebug(this.GetType(), "Start Registering BehaviourFactories");

            // Get everything that extends BehaviourFactory
            var subclasses =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.IsSubclassOf(typeof(BehaviourFactory))
                select type;

            // Register each type with the behaviour factory
            foreach (Type subclass in subclasses)
            {
                string name = subclass.Name;
                if (name.EndsWith("Factory"))
                {
                    name = name.Remove(name.Length - 7, 7);
                }
                BehaviourFactory.Register(subclass, name);
            }

            LoggingUtil.LogInfo(this.GetType(), "Finished Registering BehaviourFactories");
        }

        /*
         * Registers all the out of the box ContractRequirement classes.
         */
        void RegisterContractRequirements()
        {
            LoggingUtil.LogDebug(this.GetType(), "Start Registering ContractRequirements");

            // Get everything that extends ContractRequirement
            var subclasses =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.IsSubclassOf(typeof(ContractRequirement))
                select type;

            // Register each type with the parameter factory
            foreach (Type subclass in subclasses)
            {
                string name = subclass.Name;
                if (name.EndsWith("Requirement"))
                {
                    name = name.Remove(name.Length - 11, 11);
                }
                ContractRequirement.Register(subclass, name);
            }

            LoggingUtil.LogInfo(this.GetType(), "Finished Registering ContractRequirements");
        }

        /*
         * Loads all the contact configuration nodes and creates ContractType objects.
         */
        void LoadContractConfig()
        {
            LoggingUtil.LogDebug(this.GetType(), "Loading CONTRACT_TYPE nodes.");
            ConfigNode[] contractConfigs = GameDatabase.Instance.GetConfigNodes("CONTRACT_TYPE");

            // First pass - create all the ContractType objects
            foreach (ConfigNode contractConfig in contractConfigs)
            {
                LoggingUtil.LogDebug(this.GetType(), "First pass for node: '" + contractConfig.GetValue("name") + "'");
                // Create the initial contract type
                try
                {
                    ContractType contractType = new ContractType(contractConfig.GetValue("name"));
                }
                catch (ArgumentException)
                {
                    LoggingUtil.LogError(this.GetType(), "Couldn't load CONTRACT_TYPE '" + contractConfig.GetValue("name") + "' due to a duplicate name.");
                }
            }

            // Second pass - do the actual loading of details
            foreach (ConfigNode contractConfig in contractConfigs)
            {
                // Fetch the contractType
                string name = contractConfig.GetValue("name");
                ContractType contractType = ContractType.contractTypes[name];
                if (contractType != null)
                {
                    LoggingUtil.LogDebug(this.GetType(), "Second pass for node: '" + name + "'");
                    // Perform the load
                    try
                    {
                        contractType.Load(contractConfig);
                    }
                    catch (Exception e)
                    {
                        ContractType.contractTypes.Remove(name);
                        string err = "Error loading contract type '" + name +
                            "': " + e.Message + "\n" + e.StackTrace;
                        while (e.InnerException != null)
                        {
                            e = e.InnerException;
                            err += "\n" + e.Message + "\n" + e.StackTrace;
                        }
                        LoggingUtil.LogError(this.GetType(), err);
                    }
                }
            }

            LoggingUtil.LogInfo(this.GetType(), "Finished loading ContractTypes");
        }

        /*
         * Performs adjustments to the contract type list.  Specifically, disables contract types
         * as per configuration files and adds addtional ConfiguredContract instances based on the
         * number on contract types.
         */
        bool AdjustContractTypes()
        {
            // Don't do anything if the contract system has not yet loaded
            if (ContractSystem.ContractTypes == null)
            {
                return false;
            }

            LoggingUtil.LogDebug(this.GetType(), "Loading CONTRACT_CONFIGURATOR nodes.");
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("CONTRACT_CONFIGURATOR");

            // Build a unique list of contract types to disable, in case multiple mods try to
            // disable the same ones.
            Dictionary<string, Type> contractsToDisable = new Dictionary<string, Type>();
            foreach (ConfigNode node in nodes)
            {
                foreach (string contractType in node.GetValues("disabledContractType"))
                {
                    // No type for now
                    contractsToDisable[contractType] = null;
                }
            }

            // Figure out the types
            var subclasses =
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.IsSubclassOf(typeof(Contract))
                select type;

            // Map the string to a type
            foreach (Type subclass in subclasses)
            {
                string name = subclass.Name;
                if (contractsToDisable.ContainsKey(name))
                {
                    contractsToDisable[name] = subclass;
                }
            }

            // Start disabling!
            foreach (KeyValuePair<string, Type> p in contractsToDisable)
            {
                // Didn't find a type
                if (p.Value == null)
                {
                    LoggingUtil.LogWarning(this.GetType(), "Couldn't find ContractType '" + p.Key + "' to disable.");
                }
                else
                {
                    LoggingUtil.LogDebug(this.GetType(), "Disabling ContractType: " + p.Value.FullName + " (" + p.Value.Module + ")");
                    ContractSystem.ContractTypes.Remove(p.Value);
                }
            }

            // Now add the ConfiguredContract type
            int count = (int)(ContractType.contractTypes.Count / 4.0 + 0.5);
            for (int i = 0; i < count; i++)
            {
                ContractSystem.ContractTypes.Add(typeof(ConfiguredContract));
            }

            LoggingUtil.LogInfo(this.GetType(), "Finished Adjusting ContractTypes");

            return true;
        }
    }
}
