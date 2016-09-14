using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using KSP.UI.Screens;
using Contracts;
using FinePrint;
using ContractConfigurator.Util;

namespace ContractConfigurator
{
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class ContractConfigurator : MonoBehaviour
    {
        public static System.Version ENHANCED_UI_VERSION = new System.Version(1, 15, 0);


        private enum ReloadStep
        {
            GAME_DATABASE,
            MODULE_MANAGER,
            CLEAR_CONFIG,
            LOAD_CONFIG,
        }

        private static ContractConfigurator Instance;

        public static bool reloading = false;
        static ReloadStep reloadStep = ReloadStep.GAME_DATABASE;

        static ScreenMessage lastMessage = null;

        public static int totalContracts = 0;
        public static int successContracts = 0;
        public static int attemptedContracts = 0;

        private List<Contract> contractsToUpdate = new List<Contract>();
        private static List<Assembly> badAssemblies = new List<Assembly>();

        public static EventData<Contract, ContractParameter> OnParameterChange = new EventData<Contract, ContractParameter>("OnParameterChange");

        void Start()
        {
            DontDestroyOnLoad(this);
            Instance = this;

            PSystemManager.Instance.OnPSystemReady.Add(PSystemReady);

            OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(ParameterChange));
            GameEvents.OnTechnologyResearched.Add(new EventData<GameEvents.HostTargetAction<RDTech, RDTech.OperationResult>>.OnEvent(OnTechResearched));
        }

        void Destroy()
        {
            OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(ParameterChange));
            GameEvents.OnTechnologyResearched.Remove(new EventData<GameEvents.HostTargetAction<RDTech, RDTech.OperationResult>>.OnEvent(OnTechResearched));
        }

        void PSystemReady()
        {
            // Log version info
            var ainfoV = Attribute.GetCustomAttribute(typeof(ContractConfigurator).Assembly,
                    typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
            LoggingUtil.LogInfo(this, "Contract Configurator " + ainfoV.InformationalVersion + " loading...");

            LoggingUtil.LoadDebuggingConfig();

            RegisterParameterFactories();
            RegisterBehaviourFactories();
            RegisterContractRequirements();
            IEnumerator<YieldInstruction> iterator = LoadContractConfig();
                while (iterator.MoveNext()) { }
            DebugWindow.LoadTextures();

            LoggingUtil.LogInfo(this, "Contract Configurator " + ainfoV.InformationalVersion + " finished loading.");
        }

        void Update()
        {
            // Alt-F10 shows the contract configurator window
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.F10))
            {
                DebugWindow.showGUI = !DebugWindow.showGUI;
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
                }
            }
        }

        /// <summary>
        /// This used to be something unique to work around a bug, but the bug in stock no longer exists.  Look into removing.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="p"></param>
        private void ParameterChange(Contract c, ContractParameter p)
        {
            GameEvents.Contract.onParameterChange.Fire(c, p);
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
        }

        public void ModuleManagerPostLoad()
        {
            StartCoroutine(ContractConfiguratorReload());
        }

        private IEnumerator<YieldInstruction> ContractConfiguratorReload()
        {
            // Clear contract configurator
            reloading = true;
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
            LoggingUtil.LogDebug(this, "Start Registering ParameterFactories");

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

            LoggingUtil.LogInfo(this, "Finished Registering ParameterFactories");
        }

        /// <summary>
        /// Registers all the BehaviourFactory classes.
        /// </summary>
        void RegisterBehaviourFactories()
        {
            LoggingUtil.LogDebug(this, "Start Registering BehaviourFactories");

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

            LoggingUtil.LogInfo(this, "Finished Registering BehaviourFactories");
        }

        /// <summary>
        /// Registers all the ContractRequirement classes.
        /// </summary>
        void RegisterContractRequirements()
        {
            LoggingUtil.LogDebug(this, "Start Registering ContractRequirements");

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

            LoggingUtil.LogInfo(this, "Finished Registering ContractRequirements");
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
            LoggingUtil.LogDebug(this, "Loading CONTRACT_GROUP nodes.");
            ConfigNode[] contractGroups = GameDatabase.Instance.GetConfigNodes("CONTRACT_GROUP");

            foreach (ConfigNode groupConfig in contractGroups)
            {
                // Create the group
                string name = groupConfig.GetValue("name");
                LoggingUtil.LogInfo(this, "Loading CONTRACT_GROUP: '" + name + "'");
                ContractGroup contractGroup = null;
                try
                {
                    contractGroup = new ContractGroup(name);
                }
                catch (ArgumentException)
                {
                    LoggingUtil.LogError(this, "Couldn't load CONTRACT_GROUP '" + name + "' due to a duplicate name.");
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

            LoggingUtil.LogDebug(this, "Loading CONTRACT_TYPE nodes.");
            ConfigNode[] contractConfigs = GameDatabase.Instance.GetConfigNodes("CONTRACT_TYPE");
            totalContracts = contractConfigs.Count();

            // First pass - create all the ContractType objects
            foreach (ConfigNode contractConfig in contractConfigs)
            {
                // Create the initial contract type
                LoggingUtil.LogVerbose(this, "Pre-load for node: '" + contractConfig.GetValue("name") + "'");
                try
                {
                    ContractType contractType = new ContractType(contractConfig.GetValue("name"));
                }
                catch (ArgumentException)
                {
                    LoggingUtil.LogError(this, "Couldn't load CONTRACT_TYPE '" + contractConfig.GetValue("name") + "' due to a duplicate name.");
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
                        LoggingUtil.LogException(e);
                    }
                }
            }

            LoggingUtil.LogInfo(this, "Loaded " + successContracts + " out of " + totalContracts + " CONTRACT_TYPE nodes.");

            // Check for empty groups and warn
            foreach (ContractGroup group in ContractGroup.contractGroups.Values.Where(g => g != null))
            {
                group.CheckEmpty();
            }

            // Load other things
            MissionControlUI.GroupContainer.LoadConfig();

            // Emit settings for the menu
            SettingsBuilder.EmitSettings();

            if (!reloading && LoggingUtil.logLevel == LoggingUtil.LogLevel.DEBUG || LoggingUtil.logLevel == LoggingUtil.LogLevel.VERBOSE)
            {
                ScreenMessages.PostScreenMessage("Contract Configurator: Loaded " + successContracts + " out of " + totalContracts
                    + " contracts successfully.", 5, ScreenMessageStyle.UPPER_CENTER);
            }
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

        private void DoNothing() { }

        // Remove experimental parts when a tech is researched
        private void OnTechResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> hta)
        {
            foreach (AvailablePart p in hta.host.partsAssigned)
            {
                ResearchAndDevelopment.RemoveExperimentalPart(p);
            }
        }

        public static int ContractLimit(Contract.ContractPrestige prestige)
        {
            int level = (int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.MissionControl) *
                ScenarioUpgradeableFacilities.GetFacilityLevelCount(SpaceCenterFacility.MissionControl));
            float rep = Reputation.Instance.reputation;
            switch (prestige)
            {
                case Contract.ContractPrestige.Trivial:
                    return Math.Max(2, (int)Math.Round((rep + rep * level / 3) / 200 + 6 + level));
                case Contract.ContractPrestige.Significant:
                    return Math.Max(1, (int)Math.Round((rep + rep * level / 3) / 250 + 4 + level));
                case Contract.ContractPrestige.Exceptional:
                    return Math.Max(0, (int)Math.Round((rep + rep * level / 3) / (1000/3.0) + 2 + level));
            }
            return 0;
        }

        public static bool CanAccept(Contract contract)
        {
            int activeCount = ContractSystem.Instance.Contracts.Count(c => c != null && c.Prestige == contract.Prestige && c.ContractState == Contract.State.Active && !c.AutoAccept);
            return (activeCount < ContractConfigurator.ContractLimit(contract.Prestige));
        }
    }
}
