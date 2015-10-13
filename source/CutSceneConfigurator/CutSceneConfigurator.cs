using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

namespace CutSceneConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames, GameScenes.FLIGHT)]
    public class CutSceneConfigurator : ScenarioModule
    {
        public static CutSceneConfigurator Instance { get; private set; }

        private ApplicationLauncherButton launcherButton = null;

        public CutSceneConfigurator()
        {
            Instance = this;
            if (closeIcon == null)
            {
                closeIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/close", false);
                toolbarIcon = GameDatabase.Instance.GetTexture("CutSceneConfigurator/icons/toolbar", false);
            }
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

        private GUIStyle tipStyle;

        private void SetupStyles()
        {
            stylesSetup = true;

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
                ApplicationLauncher.AppScenes visibleScenes = ApplicationLauncher.AppScenes.FLIGHT;
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

                var ainfoV = Attribute.GetCustomAttribute(typeof(CutSceneConfigurator).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;

                windowPos.xMin = Screen.width - 336 - 14;
                windowPos = GUILayout.Window(
                    typeof(ContractConfiguratorSettings).FullName.GetHashCode(),
                    windowPos,
                    WindowGUI,
                    "Cut Scene Configurator " + ainfoV.InformationalVersion);

                GUI.depth = 0;
                DrawToolTip();
            }
        }

        private void WindowGUI(int windowID)
        {
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
    }
}
