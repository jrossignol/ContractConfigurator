using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class ContractConfiguratorSettings : ScenarioModule
    {
        #region ContractGroupDetails
        private enum ContractGroupState
        {
            DEFAULT,
            ENABLED,
            DISABLED
        }

        private class ContractGroupDetails
        {
            public ContractGroup group;
            public bool enabled = true;
            public bool expanded = false;

            public ContractGroupDetails(ContractGroup group)
            {
                this.group = group;
            }
        }
        Dictionary<ContractGroup, ContractGroupDetails> contractGroupDetails = new Dictionary<ContractGroup, ContractGroupDetails>();
        #endregion

        public static ContractConfiguratorSettings Instance { get; private set; }

        private ApplicationLauncherButton launcherButton = null;
        
        public ContractConfiguratorSettings()
        {
            Debug.Log("ContractConfiguratorSettings()");
            Instance = this;
            if (closeIcon == null)
            {
                closeIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/close", false);
                toolbarIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/toolbar", false);
            }
        }

        void Start()
        {
            Debug.Log("ContractConfiguratorSettings.Start()");
            GameEvents.onGUIApplicationLauncherReady.Add(new EventVoid.OnEvent(SetupToolbar));
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(new EventData<GameScenes>.OnEvent(TeardownToolbar));

            // Manually set up the toolbar, by the time we are started we've already missed the event
            SetupToolbar();
        }

        void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(new EventVoid.OnEvent(SetupToolbar));
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(new EventData<GameScenes>.OnEvent(TeardownToolbar));
        }

        #region Styles
        private bool stylesSetup = false;

        private static GUIStyle regularText;
        private static GUIStyle disabledText;
        private static GUIStyle toggleStyle;
        private static GUIStyle expandButtonStyle;
        private GUIStyle tipStyle;

        private void SetupStyles()
        {
            stylesSetup = true;

            regularText = new GUIStyle(GUI.skin.label);
            regularText.padding = new RectOffset(0, 0, 2, 2);
            regularText.normal.textColor = Color.white;

            disabledText = new GUIStyle(regularText);
            disabledText.normal.textColor = Color.grey;

            toggleStyle = new GUIStyle(GUI.skin.toggle);

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
            Debug.Log("ContractConfiguratorSettings.SetupToolbar()");
            if (launcherButton == null)
            {
                Debug.Log("doing toolbar setup");
                ApplicationLauncher.AppScenes visibleScenes = ApplicationLauncher.AppScenes.SPACECENTER;
                launcherButton = ApplicationLauncher.Instance.AddModApplication(ToggleWindow, ToggleWindow, null, null, null, null,
                    visibleScenes, toolbarIcon);
            }
        }

        private void TeardownToolbar(GameScenes scene)
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

                windowPos.xMin = Screen.width - 300 - 14;
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
            GUILayout.BeginVertical(GUILayout.Width(300));

            if (Event.current.type == EventType.layout)
            {
                guiContracts = ContractType.AllContractTypes;
            }
            
            foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g == null || g.parent == null).OrderBy(g => g == null ? "ZZZ" : g.name))
            {
                if (guiContracts.Any(ct => contractGroup == null ? ct.group == null : contractGroup.BelongsToGroup(ct)))
                {
                    ContractGroupLine(contractGroup);
                }
            }

            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint && GUI.tooltip != toolTip)
            {
                toolTipTime = Time.fixedTime;
                toolTip = GUI.tooltip;
                Debug.Log("tooltip set to " + toolTip);
            }
        }

        private void ContractGroupLine(ContractGroup contractGroup, int indent = 0)
        {
            if (!contractGroupDetails.ContainsKey(contractGroup))
            {
                contractGroupDetails[contractGroup] = new ContractGroupDetails(contractGroup);
            }
            ContractGroupDetails details = contractGroupDetails[contractGroup];

            GUILayout.BeginHorizontal();

            if (indent != 0)
            {
                GUILayout.Label("", GUILayout.ExpandWidth(false), GUILayout.Width(indent * 16));
            }

            if (GUILayout.Button(details.expanded ? "-" : "+", expandButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
            {
                details.expanded = !details.expanded;
            }
            string groupName = contractGroup == null ? "No Group" : contractGroup.name;
            GUILayout.Label(groupName, details.enabled ? regularText : disabledText, GUILayout.ExpandWidth(true));
            if (contractGroup != null && contractGroup.parent == null)
            {
                details.enabled = GUILayout.Toggle(details.enabled,
                    new GUIContent("", "Click to " + (details.enabled ? "disable " : "enable ") + contractGroup.name + " contracts."),
                    toggleStyle, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndHorizontal();

            if (details.expanded)
            {
                foreach (ContractGroup childGroup in ContractGroup.AllGroups.Where(g => g != null && g.parent == contractGroup).OrderBy(g => g.name))
                {
                    ContractGroupLine(childGroup, indent+1);
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
                if (Time.fixedTime > toolTipTime + 0.25)
                {
                    GUIContent tip = new GUIContent(toolTip);

                    Vector2 textDimensions = tipStyle.CalcSize(tip);
                    if (textDimensions.x > 240)
                    {
                        textDimensions.x = 240;
                        textDimensions.y = tipStyle.CalcHeight(tip, 240);
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
            foreach (ContractGroupDetails details in contractGroupDetails.Values)
            {
                ConfigNode groupNode = new ConfigNode("CONTRACT_GROUP");
                node.AddNode(groupNode);

                groupNode.AddValue("group", details.group.name);
                groupNode.AddValue("enabled", details.enabled);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            foreach (ConfigNode groupNode in node.GetNodes("CONTRACT_GROUP"))
            {
                ContractGroup group = ConfigNodeUtil.ParseValue<ContractGroup>(groupNode, "group");

                ContractGroupDetails details = new ContractGroupDetails(group);
                details.enabled = ConfigNodeUtil.ParseValue<bool>(groupNode, "enabled");
            }
        }

        #endregion

        public static bool IsEnabled(ContractGroup group)
        {
            if (Instance != null && Instance.contractGroupDetails.ContainsKey(group))
            {
                return Instance.contractGroupDetails[group].enabled;
            }
            return true;
        }
    }
}
