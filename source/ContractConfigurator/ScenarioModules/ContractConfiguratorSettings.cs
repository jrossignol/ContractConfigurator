using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI.Screens;
using Contracts;

namespace ContractConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class ContractConfiguratorSettings : ScenarioModule
    {
        #region ContractGroupDetails
        private class ContractGroupDetails
        {
            public ContractGroup group;
            public Module module;
            public bool enabled = true;
            public bool expanded = false;

            public ContractGroupDetails(ContractGroup group)
            {
                this.group = group;
            }

            public ContractGroupDetails(Module module)
            {
                this.module = module;
            }
        }
        Dictionary<string, ContractGroupDetails> contractGroupDetails = new Dictionary<string, ContractGroupDetails>();
        static Dictionary<Module, ContractGroupDetails> stockGroupDetails = new Dictionary<Module, ContractGroupDetails>();

        private class StockContractDetails
        {
            public Type contractType;
            public bool enabled = true;

            public StockContractDetails(Type contractType)
            {
                this.contractType = contractType;
                enabled = ContractDisabler.IsEnabled(contractType);

                if (!stockGroupDetails.ContainsKey(contractType.Module))
                {
                    stockGroupDetails[contractType.Module] = new ContractGroupDetails(contractType.Module);
                }
            }
        }
        Dictionary<Type, StockContractDetails> stockContractDetails = new Dictionary<Type, StockContractDetails>();
        #endregion

        public static ContractConfiguratorSettings Instance { get; private set; }

        private ApplicationLauncherButton launcherButton = null;
        
        public ContractConfiguratorSettings()
        {
            Instance = this;
            if (closeIcon == null)
            {
                closeIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/close", false);
                toolbarIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/toolbar", false);
            }

            SeedStockContractDetails();
        }

        void Start()
        {
            SetupToolbar();
        }

        void OnDestroy()
        {
            TeardownToolbar();
        }

        #region Styles
        private bool stylesSetup = false;

        private static GUIStyle sectionText;
        private static GUIStyle groupRegularText;
        private static GUIStyle groupDisabledText;
        private static GUIStyle groupToggleStyle;
        private static GUIStyle contractRegularText;
        private static GUIStyle contractDisabledText;
        private static GUIStyle contractToggleStyle;
        private static GUIStyle expandButtonStyle;
        private GUIStyle tipStyle;

        private void SetupStyles()
        {
            stylesSetup = true;

            sectionText = new GUIStyle(GUI.skin.label);
            sectionText.normal.textColor = Color.white;
            sectionText.fontStyle = FontStyle.Bold;

            groupRegularText = new GUIStyle(GUI.skin.label);
            groupRegularText.padding = new RectOffset(0, 0, 2, 2);
            groupRegularText.normal.textColor = Color.white;

            groupDisabledText = new GUIStyle(groupRegularText);
            groupDisabledText.normal.textColor = Color.grey;

            contractRegularText = new GUIStyle(groupRegularText);
            contractRegularText.padding = new RectOffset(0, 0, 0, 0);

            contractDisabledText = new GUIStyle(groupDisabledText);
            contractDisabledText.padding = new RectOffset(0, 0, 0, 0);

            groupToggleStyle = new GUIStyle(GUI.skin.toggle);
            contractToggleStyle = new GUIStyle(groupToggleStyle);
            contractToggleStyle.padding.top -= 2;
            contractToggleStyle.padding.bottom -= 2;

            expandButtonStyle = new GUIStyle(GUI.skin.button);
            expandButtonStyle.padding = new RectOffset(-2, 0, 4, 0);

            // Tooltips
            tipStyle = new GUIStyle(GUI.skin.box);
            tipStyle.wordWrap = true;
            tipStyle.stretchHeight = true;
            tipStyle.normal.textColor = Color.white;
        }
        #endregion

        #region GUI
        private Rect windowPos = new Rect(580f, 40f, 1f, 1f);
        private bool showGUI = false;
        public Vector2 scrollPosition;
        private static IEnumerable<ContractType> guiContracts;

        private Rect tooltipPosition;
        private string toolTip;
        private double toolTipTime;

        private static Texture2D closeIcon;
        private static Texture2D toolbarIcon;

        private void ToggleWindow()
        {
            showGUI = !showGUI;
        }

        private void SetupToolbar()
        {
            if (launcherButton == null)
            {
                ApplicationLauncher.AppScenes visibleScenes = ApplicationLauncher.AppScenes.SPACECENTER;
                launcherButton = ApplicationLauncher.Instance.AddModApplication(ToggleWindow, ToggleWindow, null, null, null, null,
                    visibleScenes, toolbarIcon);
            }
        }

        private void TeardownToolbar()
        {
            if (launcherButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(launcherButton);
                launcherButton = null;
            }
        }

        void OnGUI()
        {
            if (showGUI)
            {
                if (!stylesSetup)
                {
                    SetupStyles();
                }

                GUI.skin = HighLogic.Skin;

                var ainfoV = Attribute.GetCustomAttribute(typeof(ContractConfigurator).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;

                windowPos.xMin = Screen.width - 336 - 14;
                windowPos.yMin = Screen.height - windowPos.height - 40f;
                windowPos.yMax = Screen.height - 40f;
                windowPos = GUILayout.Window(
                    typeof(ContractConfiguratorSettings).FullName.GetHashCode(),
                    windowPos,
                    WindowGUI,
                    "Contract Configurator " + ainfoV.InformationalVersion + " Settings");

                GUI.depth = 0;
                DrawToolTip();
            }
        }

        private void WindowGUI(int windowID)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Width(336), GUILayout.Height(640));
            GUILayout.BeginVertical(GUILayout.Width(300), GUILayout.ExpandWidth(false));

            GUILayout.Space(8);

            GUILayout.Label("Contract Configurator Contracts", sectionText);

            if (Event.current.type == EventType.layout)
            {
                guiContracts = ContractType.AllValidContractTypes;
            }
            
            foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g != null && g.parent == null).OrderBy(g => g == null ? "ZZZ" : g.name))
            {
                if (guiContracts.Any(ct => contractGroup == null ? ct.group == null : contractGroup.BelongsToGroup(ct)))
                {
                    ContractGroupLine(contractGroup);
                }
            }

            GUILayout.Label("Standard Contracts", sectionText);
            StockGroupLine();

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint && GUI.tooltip != toolTip)
            {
                toolTipTime = Time.fixedTime;
                toolTip = GUI.tooltip;
            }
        }

        private void ContractGroupLine(ContractGroup contractGroup, int indent = 0)
        {
            string identifier = contractGroup == null ? "" : contractGroup.name;
            if (!contractGroupDetails.ContainsKey(identifier))
            {
                contractGroupDetails[identifier] = new ContractGroupDetails(contractGroup);
            }
            ContractGroupDetails details = contractGroupDetails[identifier];

            GUILayout.BeginHorizontal();

            GUILayout.Label("", contractRegularText, GUILayout.ExpandWidth(false), GUILayout.Width((indent+1) * 16));

            string groupName = contractGroup == null ? "No Group" : contractGroup.displayName;
            GUILayout.Label(groupName, details.enabled ? contractRegularText : contractDisabledText, GUILayout.ExpandWidth(true));
            if (contractGroup != null && contractGroup.parent == null)
            {
                bool enabled = GUILayout.Toggle(details.enabled,
                    new GUIContent("", "Click to " + (details.enabled ? "disable " : "enable ") + contractGroup.displayName + " contracts."),
                    contractToggleStyle, GUILayout.ExpandWidth(false));

                if (enabled != details.enabled)
                {
                    details.enabled = enabled;
                    if (enabled)
                    {
                        foreach (KeyValuePair<Type, StockContractDetails> pair in stockContractDetails.
                            Where(p => ContractDisabler.DisablingGroups(p.Key).Contains(contractGroup)))
                        {
                            pair.Value.enabled = false;
                            ContractDisabler.SetContractState(pair.Key, false);
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<Type, StockContractDetails> pair in stockContractDetails.
                            Where(p => ContractDisabler.DisablingGroups(p.Key).Contains(contractGroup) &&
                                ContractDisabler.DisablingGroups(p.Key).All(g => !IsEnabled(g))))
                        {
                            pair.Value.enabled = true;
                            ContractDisabler.SetContractState(pair.Key, true);
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
        
        private void StockGroupLine()
        {
            foreach (KeyValuePair<Module, ContractGroupDetails> gpair in stockGroupDetails.OrderBy(p => p.Key.Name == "Assembly-CSharp.dll" ? "ZZZ" : p.Key.Name))
            {
                Module module = gpair.Key;
                ContractGroupDetails groupDetails = gpair.Value;

                GUILayout.BeginHorizontal();

                if (GUILayout.Button(groupDetails.expanded ? "-" : "+", expandButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    groupDetails.expanded = !groupDetails.expanded;
                }
                string groupName = module.Name == "Assembly-CSharp.dll" ? "Stock Contracts" : module.Name.Remove(module.Name.Length - 4);
                GUILayout.Label(groupName, groupRegularText, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (groupDetails.expanded)
                {
                    foreach (KeyValuePair<Type, StockContractDetails> pair in stockContractDetails.Where(p => p.Key.Module == module).OrderBy(p => p.Key.Name))
                    {
                        Type subclass = pair.Key;
                        StockContractDetails details = pair.Value;

                        LoggingUtil.LogDebug(this, "zzz Contract type = " + subclass.Name + ", enabled = " + details.enabled + ", in list = " + ContractSystem.ContractTypes.Contains(subclass));

                        string hintText;
                        IEnumerable<ContractGroup> disablingGroups = ContractDisabler.DisablingGroups(subclass);
                        if (disablingGroups.Any())
                        {
                            hintText = subclass.Name + " disabled by: " +
                                string.Join(", ", disablingGroups.Select(g => g == null ? "unknown" : g.displayName).ToArray()) + "\n";
                            hintText += "Click to " + (details.enabled ? "disable " : "re-enable ") + subclass.Name + ".";
                        }
                        else
                        {
                            hintText = "Click to " + (details.enabled ? "disable " : "enable ") + subclass.Name + ".";
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("", contractRegularText, GUILayout.ExpandWidth(false), GUILayout.Width(32));
                        GUILayout.Label(new GUIContent(subclass.Name, hintText), details.enabled ? contractRegularText : contractDisabledText, GUILayout.ExpandWidth(true));

                        bool newState = GUILayout.Toggle(details.enabled, new GUIContent("", hintText),
                            contractToggleStyle, GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        if (newState != details.enabled)
                        {
                            details.enabled = newState;
                            ContractDisabler.SetContractState(subclass, details.enabled);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw tool tips.
        /// </summary>
        private void DrawToolTip()
        {
            if (!string.IsNullOrEmpty(toolTip))
            {
                if (Time.fixedTime > toolTipTime + 0.10)
                {
                    GUIContent tip = new GUIContent(toolTip);

                    Vector2 textDimensions = tipStyle.CalcSize(tip);
                    if (textDimensions.x > 320)
                    {
                        textDimensions.x = 320;
                        textDimensions.y = tipStyle.CalcHeight(tip, 320);
                    }
                    tooltipPosition.width = textDimensions.x;
                    tooltipPosition.height = textDimensions.y;
                    tooltipPosition.x = Event.current.mousePosition.x + tooltipPosition.width > Screen.width ?
                        Screen.width - tooltipPosition.width : Event.current.mousePosition.x;
                    tooltipPosition.y = Event.current.mousePosition.y + 20;

                    GUI.Label(tooltipPosition, tip, tipStyle);
                }
            }
        }

        #endregion

        #region Settings

        public override void OnSave(ConfigNode node)
        {
            try
            {
                foreach (ContractGroupDetails details in contractGroupDetails.Values.Where(d => d.group != null))
                {
                    ConfigNode groupNode = new ConfigNode("CONTRACT_GROUP");
                    node.AddNode(groupNode);

                    groupNode.AddValue("group", details.group.name);
                    groupNode.AddValue("enabled", details.enabled);
                }

                foreach (StockContractDetails details in stockContractDetails.Values.Where(d => d.contractType != null))
                {
                    ConfigNode stateNode = new ConfigNode("CONTRACT_STATE");
                    node.AddNode(stateNode);

                    stateNode.AddValue("type", details.contractType.Name);
                    stateNode.AddValue("enabled", details.enabled);
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error saving ContractConfiguratorSettings to persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_SAVE, e, "ContractConfiguratorSettings");
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                foreach (ConfigNode groupNode in node.GetNodes("CONTRACT_GROUP"))
                {
                    string groupName = groupNode.GetValue("group");

                    if (ContractGroup.contractGroups.ContainsKey(groupName))
                    {
                        ContractGroup group = ContractGroup.contractGroups[groupName];

                        ContractGroupDetails details = new ContractGroupDetails(group);
                        details.enabled = ConfigNodeUtil.ParseValue<bool>(groupNode, "enabled");

                        contractGroupDetails[group.name] = details;
                    }
                    else
                    {
                        LoggingUtil.LogWarning(this, "Couldn't find contract group with name '" + groupName + "'");
                    }
                }

                foreach (ConfigNode stateNode in node.GetNodes("CONTRACT_STATE"))
                {
                    string typeName = stateNode.GetValue("type");
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        Type contractType = null;
                        try
                        {
                            contractType = ConfigNodeUtil.ParseTypeValue(typeName);
                            StockContractDetails details = new StockContractDetails(contractType);
                            details.enabled = ConfigNodeUtil.ParseValue<bool>(stateNode, "enabled");

                            stockContractDetails[contractType] = details;

                            ContractDisabler.SetContractState(contractType, details.enabled);
                        }
                        catch (ArgumentException)
                        {
                            LoggingUtil.LogWarning(this, "Couldn't find contract type with name '" + typeName + "'");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error loading ContractConfiguratorSettings from persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_LOAD, e, "ContractConfiguratorSettings");
            }
        }

        #endregion

        public static bool IsEnabled(ContractGroup group)
        {
            string identifier = group == null ? "" : group.name;
            if (Instance != null && Instance.contractGroupDetails.ContainsKey(identifier))
            {
                return Instance.contractGroupDetails[identifier].enabled;
            }
            return true;
        }

        private void SeedStockContractDetails()
        {
            // Enable everything
            foreach (Type subclass in ContractConfigurator.GetAllTypes<Contract>().Where(t => t != null && !t.Name.StartsWith("ConfiguredContract")))
            {
                ContractDisabler.SetContractState(subclass, true);
            }

            // Make sure that the initial state has been correctly set
            ContractDisabler.contractsDisabled = false;
            ContractDisabler.DisableContracts();

            foreach (Type subclass in ContractConfigurator.GetAllTypes<Contract>().Where(t => t != null && !t.Name.StartsWith("ConfiguredContract")))
            {
                if (!stockContractDetails.ContainsKey(subclass))
                {
                    stockContractDetails[subclass] = new StockContractDetails(subclass);
                    if (ContractSystem.ContractTypes != null)
                    {
                        stockContractDetails[subclass].enabled = ContractSystem.ContractTypes.Any(t => t == subclass);
                    }
                }
            }
        }
    }
}
