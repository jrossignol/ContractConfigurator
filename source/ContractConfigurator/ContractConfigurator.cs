using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class ContractConfigurator : MonoBehaviour
    {
        static bool loaded = false;

        void Start()
        {
            DontDestroyOnLoad(this);

            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !loaded)
            {
                RegisterParameterFactories();
                RegisterContractRequirements();
                LoadContractConfig();
                loaded = true;
            }
        }

        /*
         * Registers all the out of the box ParameterFactory classes.
         */
        void RegisterParameterFactories()
        {
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
        }

        /*
         * Registers all the out of the box ContractRequirement classes.
         */
        void RegisterContractRequirements()
        {
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
        }

        /*
         * Loads all the contact configuration nodes and creates ContractType objects.
         */
        void LoadContractConfig()
        {
            Debug.Log("ContractConfigurator: Loading CONTRACT_TYPE nodes.");
            ConfigNode[] contractConfigs = GameDatabase.Instance.GetConfigNodes("CONTRACT_TYPE");

            // First pass - create all the ContractType objects
            foreach (ConfigNode contractConfig in contractConfigs)
            {
                Debug.Log("ContractConfigurator: First pass for node: '" + contractConfig.GetValue("name") + "'");

                // Create the initial contract type
                try
                {
                    ContractType contractType = new ContractType(contractConfig.GetValue("name"));
                }
                catch (ArgumentException)
                {
                    Debug.LogError("ContractConfigurator: Couldn't load CONTRACT_TYPE '" +
                        contractConfig.GetValue("name") + "' due to a duplicate name.");
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
                    Debug.Log("ContractConfigurator: Second pass for node: '" + name + "'");

                    // Perform the load
                    try
                    {
                        contractType.Load(contractConfig);
                    }
                    catch (Exception e)
                    {
                        ContractType.contractTypes.Remove(name);
                        Debug.LogError("ContractConfigurator: Error loading contract type '" + name +
                            "': " + e.Message + "\n" + e.StackTrace);
                    }
                }
            }
        }
    }
}
