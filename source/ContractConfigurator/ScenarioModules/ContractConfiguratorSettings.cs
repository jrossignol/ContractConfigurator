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
        GameScenes.SPACECENTER)]
    public class ContractConfiguratorSettings : ScenarioModule
    {
        public static ContractConfiguratorSettings Instance { get; private set; }

        private ApplicationLauncherButton launcherButton = null;
        
        private Rect windowPos = new Rect(580f, 40f, 1f, 1f);
        private bool showGUI = false;
        private static IEnumerable<ContractType> guiContracts;

        private static Texture2D closeIcon;
        private static Texture2D toolbarIcon;

        private bool stylesSetup = false;
        private GUIStyle windowStyle;

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
                GUI.skin = HighLogic.Skin;

                if (!stylesSetup)
                {
                    stylesSetup = true;
                    windowStyle = new GUIStyle(GUI.skin.window);
                    windowStyle.normal.textColor = Color.white;
                }

                GUI.skin.window = windowStyle;

                   var ainfoV = Attribute.GetCustomAttribute(typeof(ContractConfigurator).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;

                windowPos.xMin = Screen.width - 300 - 12;
                windowPos = GUILayout.Window(
                    typeof(ContractConfiguratorSettings).FullName.GetHashCode(),
                    windowPos,
                    WindowGUI,
                    "Contract Configurator " + ainfoV.InformationalVersion + " Settings");
            }
        }

        private void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical(GUILayout.Width(300));

            if (Event.current.type == EventType.layout)
            {
                guiContracts = ContractType.AllContractTypes;
            }
            
            foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g == null || g.parent == null))
            {
                if (guiContracts.Any(ct => contractGroup == null ? ct.group == null : contractGroup.BelongsToGroup(ct)))
                {
                    GUILayout.Label(contractGroup == null ? "No Group" : contractGroup.name);
                }
            }

            GUILayout.EndVertical();
        }
    }
}
