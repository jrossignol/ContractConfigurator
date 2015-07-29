using System;
using System.Collections.Generic;
using System.IO;
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
        public static string Win64WarningFileName
        {
            get
            {
                return string.Join(Path.DirectorySeparatorChar.ToString(), new string[] { KSPUtil.ApplicationRootPath, "GameData", "ContractConfigurator", "Win64Warning.cfg" });
            }
        }

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
        private double lastContractsAppCheck = 0.0;

        private List<Contract> contractsToUpdate = new List<Contract>();
        private static List<Assembly> badAssemblies = new List<Assembly>();

        public static EventData<Contract, ContractParameter> OnParameterChange = new EventData<Contract, ContractParameter>("OnParameterChange");

        static int[] foo = { 1, 2, 3 };

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
                // Log version info
                var ainfoV = Attribute.GetCustomAttribute(typeof(ContractConfigurator).Assembly,
                    typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                LoggingUtil.LogInfo(this, "Contract Configurator " + ainfoV.InformationalVersion + " loading...");

                // Check for Win64
                DoWin64Check();

                LoggingUtil.LoadDebuggingConfig();

                RegisterParameterFactories();
                RegisterBehaviourFactories();
                RegisterContractRequirements();
                loading = true;
                IEnumerator<YieldInstruction> iterator = LoadContractConfig();
                while (iterator.MoveNext()) { }
                DebugWindow.LoadTextures();

                LoggingUtil.LogInfo(this, "Contract Configurator " + ainfoV.InformationalVersion + " finished loading.");
            }
            // Make contract type adjustments
            else if (HighLogic.LoadedScene == GameScenes.SPACECENTER && !contractTypesAdjusted)
            {
                ContractDisabler.DisableContracts();

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
                ContractsApp.Instance.appLauncherButton != null)
            {
                if (UnityEngine.Time.fixedTime - lastContractsAppCheck > 0.5)
                {
                    lastContractsAppCheck = UnityEngine.Time.fixedTime;
                    GenericAppFrame contractsAppFrame = UnityEngine.Object.FindObjectOfType<GenericAppFrame>();
                    if (contractsAppFrame != null && contractsAppFrame.gameObject.activeSelf &&
                        contractsAppFrame.header.text == "Contracts")
                    {
                        contractsAppVisible = true;
                    }
                }
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
                ContractsApp.Instance.appLauncherButton != null)
            {
                GenericAppFrame contractsAppFrame = UnityEngine.Object.FindObjectOfType<GenericAppFrame>();
                if (contractsAppFrame != null && contractsAppFrame.gameObject.activeSelf &&
                    contractsAppFrame.header.text == "Contracts")
                {
                    contractsAppVisible = true;
                }
                else
                {
                    contractsAppVisible = false;
                }
            }
            // Not visible
            else
            {
                contractsAppVisible = false;
            }

            // Add the contract to the list of ones to update
            contractsToUpdate.AddUnique(c);

            // Also update contracts window plus title
            ContractsWindow.SetParameterTitle(p, p.Title);
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
            foreach (Type subclass in GetAllTypes<ParameterFactory>().Where(t => !t.IsAbstract))
            {
                string name = subclass.Name;
                if (name.EndsWith("Factory"))
                {
                    name = name.Remove(name.Length - 7, 7);
                }

                try
                {
                    ParameterFactory.Register(subclass, name);
                }
                catch (Exception e)
                {
                    LoggingUtil.LogError(this, "Error registering parameter factory " + subclass.Name);
                    LoggingUtil.LogException(e);
                }
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
            foreach (Type subclass in GetAllTypes<BehaviourFactory>().Where(t => !t.IsAbstract))
            {
                string name = subclass.Name;
                if (name.EndsWith("Factory"))
                {
                    name = name.Remove(name.Length - 7, 7);
                }

                try
                {
                    BehaviourFactory.Register(subclass, name);
                }
                catch (Exception e)
                {
                    LoggingUtil.LogError(this, "Error registering behaviour factory " + subclass.Name);
                    LoggingUtil.LogException(e);
                }
            }

            LoggingUtil.LogInfo(this.GetType(), "Finished Registering BehaviourFactories");
        }

        /// <summary>
        /// Registers all the ContractRequirement classes.
        /// </summary>
        void RegisterContractRequirements()
        {
            LoggingUtil.LogDebug(this.GetType(), "Start Registering ContractRequirements");

            // Register each type
            foreach (Type subclass in GetAllTypes<ContractRequirement>().Where(t => !t.IsAbstract))
            {
                string name = subclass.Name;
                if (name.EndsWith("Requirement"))
                {
                    name = name.Remove(name.Length - 11, 11);
                }

                try
                {
                    ContractRequirement.Register(subclass, name);
                }
                catch (Exception e)
                {
                    LoggingUtil.LogError(this, "Error registering contract requirement " + subclass.Name);
                    LoggingUtil.LogException(e);
                }
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
                        ConfigNodeUtil.ClearCache(true);
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
                if (contractType != null && !contractType.loaded)
                {
                    LoggingUtil.LogDebug(this.GetType(), "Loading CONTRACT_TYPE: '" + name + "'");
                    // Perform the load
                    try
                    {
                        ConfigNodeUtil.ClearCache(true);
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

            // Check for empty groups and warn
            foreach (ContractGroup group in ContractGroup.contractGroups.Values.Where(g => g != null))
            {
                group.CheckEmpty();
            }

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
        static bool AdjustContractTypes()
        {
            if (ContractSystem.Instance == null)
            {
                return false;
            }

            // Add the ConfiguredContract type
            int countByType = (int)(Math.Pow(ContractType.AllValidContractTypes.Count(), 0.6) / 2.0);
            int countByGroup = (int)(Math.Pow(ContractGroup.AllGroups.Count(g => g != null && g.parent == null), 0.7) * 1.5);
            int count = Math.Max(countByGroup, countByType);
            LoggingUtil.LogDebug(typeof(ContractConfigurator), "Setting ConfiguredContract count to " + count);

            for (int i = 1; i < count; i++)
            {
                ContractSystem.ContractTypes.Add(typeof(ConfiguredContract));
            }

            LoggingUtil.LogInfo(typeof(ContractConfigurator), "Finished Adjusting ContractTypes");

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
                    // Only log once
                    if (!badAssemblies.Contains(assembly))
                    {
                        LoggingUtil.LogException(new Exception("Error loading types from assembly " + assembly.FullName, e));
                        badAssemblies.Add(assembly);
                    }
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

        public bool IsWin64()
        {
            // This makes no sense
            return !Util.Version.IsWin64();
        }

        private void DoWin64Check()
        {
            if (Util.Version.IsWin64() || !IsWin64())
            {
                var ainfoV = Attribute.GetCustomAttribute(typeof(ContractConfigurator).Assembly,
                    typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;

                if (File.Exists(Win64WarningFileName))
                {
                    ConfigNode configNode = ConfigNode.Load(Win64WarningFileName);
                    string version = configNode.GetValue("version");
                    if (version == ainfoV.InformationalVersion)
                    {
                        return;
                    }
                }

                string title = "Contract Configurator Win64 Support";
                string message = "Contract Configurator is not officially supported on Windown 64-bit builds of KSP " +
                    "due to wildly random bugs that take a huge amount of my (nightingale's) time to investigate.  " +
                    "It will continue to work as normal, but please do not request support for any issues " +
                    "unless they can be reproduced in a supported build.";
                DialogOption dialogOption = new DialogOption("Okay", new Callback(DoNothing), true);
                PopupDialog.SpawnPopupDialog(new MultiOptionDialog(message, title, HighLogic.Skin, dialogOption), false, HighLogic.Skin);

                ConfigNode node = new ConfigNode("CC_WIN64_WARNING");
                node.AddValue("version", ainfoV.InformationalVersion);
                node.Save(Win64WarningFileName, "Contract Configurator Win64 warning configuration");

                LoggingUtil.LogWarning(this, "ContractConfigurator on Win64 detected.");
            }
        }

        private void DoNothing() { }
    }
}
