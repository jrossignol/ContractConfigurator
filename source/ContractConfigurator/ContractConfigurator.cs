using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ContractConfigurator : MonoBehaviour
    {
        private enum ReloadStep
        {
            GAME_DATABASE,
            MODULE_MANAGER,
            CLEAR_CONFIG,
            LOAD_CONFIG,
            ADJUST_TYPES,
        }

        private static ContractConfigurator Instance;

        public static bool reloading = false;
        static ReloadStep reloadStep = ReloadStep.GAME_DATABASE;

        static bool loading = false;
        static bool contractTypesAdjusted = false;

        static ScreenMessage lastMessage = null;

        public static int totalContracts = 0;
        public static int successContracts = 0;
        public static int attemptedContracts = 0;

        private bool contractsAppVisible = false;

        private List<Contract> contractsToUpdate = new List<Contract>();

        public static EventData<Contract, ContractParameter> OnParameterChange = new EventData<Contract, ContractParameter>("OnParameterChange");

        void Start()
        {
            DontDestroyOnLoad(this);
            Instance = this;
            OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(ParameterChange));
        }

        void Destroy()
        {
            OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(ParameterChange));
        }

        void Update()
        {
            // Load all the contract configurator configuration
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !loading)
            {
                LoggingUtil.LoadDebuggingConfig();

                // Log version info
                var ainfoV = Attribute.GetCustomAttribute(typeof(ExceptionLogWindow).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                LoggingUtil.LogInfo(this, "Contract Configurator " + ainfoV.InformationalVersion + " loading...");

                RegisterParameterFactories();
                RegisterBehaviourFactories();
                RegisterContractRequirements();
                loading = true;
                IEnumerator<YieldInstruction> iterator = LoadContractConfig();
                while (iterator.MoveNext()) { }
                DebugWindow.LoadTextures();

                LoggingUtil.LogInfo(this, "Contract Configurator " + ainfoV.InformationalVersion + " finished loading.");
            }
            // Try to disable the contract types
            else if ((HighLogic.LoadedScene == GameScenes.SPACECENTER) && !contractTypesAdjusted)
            {
                if (AdjustContractTypes())
                {
                    contractTypesAdjusted = true;
                }
            }

            // Alt-F9 shows the contract configurator window
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.F10))
            {
                DebugWindow.showGUI = !DebugWindow.showGUI;
            }

            // Check if the ContractsApp has just become visible
            if (!contractsAppVisible &&
                ContractsApp.Instance != null &&
                ContractsApp.Instance.appLauncherButton != null &&
                ContractsApp.Instance.cascadingList.cascadingList != null &&
                ContractsApp.Instance.cascadingList.cascadingList.gameObject.activeInHierarchy)
            {
                contractsAppVisible = true;
            }

            // Display reloading message
            if (reloading)
            {
                if (lastMessage != null)
                {
                    ScreenMessages.RemoveMessage(lastMessage);
                    lastMessage = null;
                }

                switch (reloadStep)
                {
                    case ReloadStep.GAME_DATABASE:
                        lastMessage = ScreenMessages.PostScreenMessage("Reloading game database...", Time.deltaTime,
                            ScreenMessageStyle.UPPER_CENTER);
                        break;
                    case ReloadStep.MODULE_MANAGER:
                        lastMessage = ScreenMessages.PostScreenMessage("Reloading module manager...", Time.deltaTime,
                            ScreenMessageStyle.UPPER_CENTER);
                        break;
                    case ReloadStep.CLEAR_CONFIG:
                        lastMessage = ScreenMessages.PostScreenMessage("Clearing previously loaded contract configuration...", Time.deltaTime,
                            ScreenMessageStyle.UPPER_CENTER);
                        break;
                    case ReloadStep.LOAD_CONFIG:
                        lastMessage = ScreenMessages.PostScreenMessage("Loading contract configuration (" + attemptedContracts + "/" + totalContracts + ")...", Time.deltaTime,
                            ScreenMessageStyle.UPPER_CENTER);
                        break;
                    case ReloadStep.ADJUST_TYPES:
                        lastMessage = ScreenMessages.PostScreenMessage("Adjusting contract types...", Time.deltaTime,
                            ScreenMessageStyle.UPPER_CENTER);
                        break;
                }
            }

            // Fire update events
            if (contractsAppVisible)
            {
                foreach (Contract contract in contractsToUpdate)
                {
                    if (contract.ContractState == Contract.State.Active && contract.GetType() == typeof(ConfiguredContract))
                    {
                        GameEvents.Contract.onParameterChange.Fire(contract, contract.GetParameter(0));
                    }
                }
                contractsToUpdate.Clear();
            }
        }

        private void ParameterChange(Contract c, ContractParameter p)
        {
            // ContractsApp is visible
            if (ContractsApp.Instance != null &&
                ContractsApp.Instance.appLauncherButton != null &&
                ContractsApp.Instance.cascadingList.cascadingList != null &&
                ContractsApp.Instance.cascadingList.cascadingList.gameObject.activeInHierarchy)
            {
                contractsAppVisible = true;
            }
            // Not visible
            else
            {
                contractsAppVisible = false;
            }

            // Add the contract to the list of ones to update
            contractsToUpdate.AddUnique(c);
        }

        public void OnGUI()
        {
            DebugWindow.OnGUI();
            ExceptionLogWindow.OnGUI();
        }

        /// <summary>
        /// Starts the contract reload process
        /// </summary>
        public static void StartReload()
        {
            Instance.StartCoroutine(Instance.ReloadContractTypes());
        }

        /// <summary>
        /// Reloads all contract types from the config nodes.  Also re-runs ModuleManager if it is installed.
        /// </summary>
        private IEnumerator<YieldInstruction> ReloadContractTypes()
        {
            reloading = true;
            reloadStep = ReloadStep.GAME_DATABASE;

            GameDatabase.Instance.Recompile = true;
            GameDatabase.Instance.StartLoad();

            // Wait for the reload
            while (!GameDatabase.Instance.IsReady())
            {
                yield return new WaitForEndOfFrame();
            }

            // Attempt to find module manager and do their reload
            reloadStep = ReloadStep.MODULE_MANAGER;
            var allMM = from loadedAssemblies in AssemblyLoader.loadedAssemblies
                           let assembly = loadedAssemblies.assembly
                           where assembly.GetName().Name.StartsWith("ModuleManager")
                           orderby assembly.GetName().Version descending, loadedAssemblies.path ascending
                           select loadedAssemblies;

            // Reload module manager
            if (allMM.Count() > 0)
            {
                Assembly mmAssembly = allMM.First().assembly;
                LoggingUtil.LogVerbose(this, "Reloading config using ModuleManager: " + mmAssembly.FullName);

                // Get the module manager object
                Type mmPatchType = mmAssembly.GetType("ModuleManager.MMPatchLoader");
                LoadingSystem mmPatchLoader = (LoadingSystem) FindObjectOfType(mmPatchType);

                // Do the module manager load
                mmPatchLoader.StartLoad();
                while (!mmPatchLoader.IsReady())
                {
                    yield return new WaitForEndOfFrame();

                }
            }

            // Clear contract configurator
            reloadStep = ReloadStep.CLEAR_CONFIG;
            yield return new WaitForEndOfFrame();
            ClearContractConfig();

            // Load contract configurator
            reloadStep = ReloadStep.LOAD_CONFIG;
            IEnumerator<YieldInstruction> iterator = LoadContractConfig();
            while (iterator.MoveNext())
            {
                yield return iterator.Current;
            }

            // Adjust contract types
            reloadStep = ReloadStep.ADJUST_TYPES;
            yield return new WaitForEndOfFrame();
            AdjustContractTypes();

            // We're done!
            reloading = false;
            ScreenMessages.PostScreenMessage("Loaded " + successContracts + " out of " + totalContracts
                + " contracts successfully.", 5, ScreenMessageStyle.UPPER_CENTER);

            yield return new WaitForEndOfFrame();

            DebugWindow.scrollPosition = new Vector2();
            DebugWindow.scrollPosition2 = new Vector2();
        }


        /// <summary>
        /// Registers all the ParameterFactory classes.
        /// </summary>
        void RegisterParameterFactories()
        {
            LoggingUtil.LogDebug(this.GetType(), "Start Registering ParameterFactories");

            // Register each type with the parameter factory
            foreach (Type subclass in GetAllTypes<ParameterFactory>())
            {
                string name = subclass.Name;
                if (name.EndsWith("Factory"))
                {
                    name = name.Remove(name.Length - 7, 7);
                }

                ParameterFactory.Register(subclass, name);
            }

            LoggingUtil.LogInfo(this.GetType(), "Finished Registering ParameterFactories");
        }

        /// <summary>
        /// Registers all the BehaviourFactory classes.
        /// </summary>
        void RegisterBehaviourFactories()
        {
            LoggingUtil.LogDebug(this.GetType(), "Start Registering BehaviourFactories");

            // Register each type with the behaviour factory
            foreach (Type subclass in GetAllTypes<BehaviourFactory>())
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

        /// <summary>
        /// Registers all the ContractRequirement classes.
        /// </summary>
        void RegisterContractRequirements()
        {
            LoggingUtil.LogDebug(this.GetType(), "Start Registering ContractRequirements");

            // Register each type with the parameter factory
            foreach (Type subclass in GetAllTypes<ContractRequirement>())
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

        /// <summary>
        /// Clears the contract configuration.
        /// </summary>
        void ClearContractConfig()
        {
            ContractGroup.contractGroups.Clear();
            ContractType.ClearContractTypes();
            totalContracts = successContracts = attemptedContracts = 0;
        }

        /// <summary>
        /// Loads all the contact configuration nodes and creates ContractType objects.
        /// </summary>
        private IEnumerator<YieldInstruction> LoadContractConfig()
        {
            // Load all the contract groups
            LoggingUtil.LogDebug(this.GetType(), "Loading CONTRACT_GROUP nodes.");
            ConfigNode[] contractGroups = GameDatabase.Instance.GetConfigNodes("CONTRACT_GROUP");

            foreach (ConfigNode groupConfig in contractGroups)
            {
                // Create the group
                string name = groupConfig.GetValue("name");
                LoggingUtil.LogInfo(this.GetType(), "Loading CONTRACT_GROUP: '" + name + "'");
                ContractGroup contractGroup = null;
                try
                {
                    contractGroup = new ContractGroup(name);
                }
                catch (ArgumentException)
                {
                    LoggingUtil.LogError(this.GetType(), "Couldn't load CONTRACT_GROUP '" + name + "' due to a duplicate name.");
                }

                // Peform the actual load
                if (contractGroup != null)
                {
                    bool success = false;
                    try
                    {
                        success = contractGroup.Load(groupConfig);
                    }
                    catch (Exception e)
                    {
                        Exception wrapper = new Exception("Error loading CONTRACT_GROUP '" + name + "'", e);
                        LoggingUtil.LogException(wrapper);
                    }
                    finally
                    {
                        if (!success)
                        {
                            ContractGroup.contractGroups.Remove(name);
                        }
                    }
                }
            }

            LoggingUtil.LogDebug(this.GetType(), "Loading CONTRACT_TYPE nodes.");
            ConfigNode[] contractConfigs = GameDatabase.Instance.GetConfigNodes("CONTRACT_TYPE");
            totalContracts = contractConfigs.Count();

            // First pass - create all the ContractType objects
            foreach (ConfigNode contractConfig in contractConfigs)
            {
                // Create the initial contract type
                LoggingUtil.LogVerbose(this.GetType(), "Pre-load for node: '" + contractConfig.GetValue("name") + "'");
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
                attemptedContracts++;
                yield return new WaitForEndOfFrame();

                // Fetch the contractType
                string name = contractConfig.GetValue("name");
                ContractType contractType = ContractType.GetContractType(name);
                if (contractType != null)
                {
                    LoggingUtil.LogDebug(this.GetType(), "Loading CONTRACT_TYPE: '" + name + "'");
                    // Perform the load
                    try
                    {
                        contractType.Load(contractConfig);
                        if (contractType.enabled)
                        {
                            successContracts++;
                        }
                    }
                    catch (Exception e)
                    {
                        Exception wrapper = new Exception("Error loading CONTRACT_TYPE '" + name + "'", e);
                        LoggingUtil.LogException(wrapper);
                    }
                }
            }

            LoggingUtil.LogInfo(this.GetType(), "Loaded " + successContracts + " out of " + totalContracts + " CONTRACT_TYPE nodes.");

            if (!reloading && LoggingUtil.logLevel == LoggingUtil.LogLevel.DEBUG || LoggingUtil.logLevel == LoggingUtil.LogLevel.VERBOSE)
            {
                ScreenMessages.PostScreenMessage("Contract Configurator: Loaded " + successContracts + " out of " + totalContracts
                    + " contracts successfully.", 5, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        /// <summary>
        /// Performs adjustments to the contract type list.  Specifically, disables contract types
        /// as per configuration files and adds addtional ConfiguredContract instances based on the
        /// number on contract types.
        /// </summary>
        /// <returns>Whether the changes took place</returns>
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

            // Map the string to a type
            foreach (Type subclass in GetAllTypes<Contract>())
            {
                string name = subclass.Name;
                if (contractsToDisable.ContainsKey(name))
                {
                    contractsToDisable[name] = subclass;
                }
            }

            // Start disabling!
            int disabledCounter = 0;
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
                    disabledCounter++;
                }
            }

            LoggingUtil.LogInfo(this.GetType(), "Disabled " + disabledCounter + " ContractTypes.");

            // Now add the ConfiguredContract type
            int count = (int)(ContractType.AllValidContractTypes.Count() / 3.0 + 0.5);
            for (int i = 0; i < count; i++)
            {
                ContractSystem.ContractTypes.Add(typeof(ConfiguredContract));
            }

            LoggingUtil.LogInfo(this.GetType(), "Finished Adjusting ContractTypes");

            return true;
        }

        public static IEnumerable<Type> GetAllTypes<T>()
        {
            // Get everything that extends the given type
            List<Type> allTypes = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                IEnumerable<Type> types = null;
                try
                {
                    types = from type in assembly.GetTypes() where (type.IsSubclassOf(typeof(T)) || type.GetInterface(typeof(T).Name) != null) select type;
                }
                catch (Exception e)
                {
                    LoggingUtil.LogWarning(typeof(ContractConfigurator), "Error loading types from assembly " + assembly.FullName);
                    LoggingUtil.LogException(e);
                    continue;
                }

                foreach (Type t in types)
                {
                    Type foundType = t;

                    if (foundType != null)
                    {
                        yield return foundType;
                    }
                }
            }
        }
    }
}
