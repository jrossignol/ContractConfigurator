using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.CutScene;

namespace CutSceneConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames, GameScenes.FLIGHT)]
    public class CutSceneConfigurator : ScenarioModule
    {
        private enum Modes
        {
            Camera,
            Actor,
            Action
        }

        public static CutSceneConfigurator Instance { get; private set; }

        private ApplicationLauncherButton launcherButton = null;

        public CutSceneConfigurator()
        {
            Instance = this;
            if (closeIcon == null)
            {
                closeIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/close", false);
                toolbarIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/cutscene", false);
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
        private GUIStyle toggleStyle;
        private GUIStyle toggleCenteredStyle;
        private GUIStyle selectedItemStyle;

        private void SetupStyles()
        {
            stylesSetup = true;

            selectedItemStyle = new GUIStyle(HighLogic.Skin.button);
            selectedItemStyle.normal = selectedItemStyle.onActive;
            toggleCenteredStyle = new GUIStyle(HighLogic.Skin.button);
            toggleCenteredStyle.padding = new RectOffset(4, 4, 4, 4);
            toggleCenteredStyle.normal.textColor = Color.white;
            toggleCenteredStyle.hover.textColor = Color.white;
            toggleCenteredStyle.active.textColor = Color.white;
            toggleCenteredStyle.focused.textColor = Color.white;

            toggleStyle = new GUIStyle(toggleCenteredStyle);
            toggleStyle.alignment = TextAnchor.MiddleLeft;
            toggleStyle.fontStyle = FontStyle.Normal;

            // Tooltips
            tipStyle = new GUIStyle(GUI.skin.box);
            tipStyle.wordWrap = true;
            tipStyle.stretchHeight = true;
            tipStyle.normal.textColor = Color.white;
        }
        #endregion

        #region GUI

        public const float LIST_HEIGHT = 540f;
        public const float DETAIL_LABEL_WIDTH = 140f;
        public const float DETAIL_ENTRY_WIDTH = 322f;

        private Rect windowPos = new Rect(88f, 60f, 1f, 1f);
        private static Rect rmWindowPos = new Rect(Screen.width / 2.0f - 140f, Screen.height / 2.0f - 40f, 280f, 80f);
        private bool showGUI = false;
        public Vector2 scrollPosition;
        public Vector2 detailScrollPosition;

        private Rect tooltipPosition;
        private string toolTip;
        private double toolTipTime;

        private CutSceneItem currentItem;
        private CutSceneDefinition currentCutScene;
        private CutSceneAction currentAction;
        private CutSceneCamera currentCamera;
        private Actor currentActor;
        private bool deleteCurrent = false;
        private Modes currentMode = Modes.Action;

        private static Texture2D closeIcon;
        private static Texture2D toolbarIcon;

        private void ToggleWindow()
        {
            showGUI = !showGUI;
        }

        private void SetupToolbar()
        {
            if (launcherButton == null && LoggingUtil.GetLogLevel(typeof(CutSceneConfigurator)) == LoggingUtil.LogLevel.DEBUG)
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

                // Main window
                windowPos = GUILayout.Window(
                    typeof(CutSceneConfigurator).FullName.GetHashCode(),
                    windowPos,
                    WindowGUI,
                    "Cut Scene Configurator " + ainfoV.InformationalVersion);

                // Add the close icon
                if (GUI.Button(new Rect(windowPos.xMax - 18, windowPos.yMin + 2, 16, 16), closeIcon, GUI.skin.label))
                {
                    showGUI = false;
                }

                // Show the delete confirmation dialog
                if (deleteCurrent)
                {
                    rmWindowPos = GUILayout.Window(
                        typeof(CutSceneConfigurator).FullName.GetHashCode() + 1,
                        rmWindowPos,
                        DeleteGUI,
                        "Delete Cut Scene Action");

                    // Add the close icon
                    if (GUI.Button(new Rect(rmWindowPos.xMax - 18, rmWindowPos.yMin + 2, 16, 16), closeIcon, GUI.skin.label))
                    {
                        deleteCurrent = false;
                    }
                }

                GUI.depth = 0;
                DrawToolTip();
            }
        }

        private void WindowGUI(int windowID)
        {
            // TODO - real loading
            if (currentCutScene == null)
            {
                currentCutScene = new CutSceneDefinition();
                currentCutScene.Load("ContractConfigurator/CutScene/CameraTestCutScene.cfg");
            }

            GUILayout.BeginHorizontal();

            const int LIST_WIDTH = 500;
            GUILayout.BeginVertical(GUILayout.Width(LIST_WIDTH));

            // The different modes
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(currentMode == Modes.Camera, "Cameras", toggleCenteredStyle))
            {
                if (currentMode != Modes.Camera)
                {
                    currentMode = Modes.Camera;
                    scrollPosition = new Vector2(0, 0);
                    currentItem = currentCamera;
                }
            }
            if (GUILayout.Toggle(currentMode == Modes.Actor, "Actors", toggleCenteredStyle))
            {
                if (currentMode != Modes.Actor)
                {
                    currentMode = Modes.Actor;
                    scrollPosition = new Vector2(0, 0);
                    currentItem = currentActor;
                }
            }
            if (GUILayout.Toggle(currentMode == Modes.Action, "Actions", toggleCenteredStyle))
            {
                if (currentMode != Modes.Action)
                {
                    currentMode = Modes.Action;
                    scrollPosition = new Vector2(0, 0);
                    currentItem = currentAction;
                }
            }
            GUILayout.EndHorizontal();

            // Display the listing of entities
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.Height(LIST_HEIGHT));
            int currentIndex = -1;
            int i = 0;
            int listCount = 0;

            // Display the listing of cameras
            if (currentMode == Modes.Camera)
            {
                listCount = currentCutScene.cameras.Count;
                foreach (CutSceneCamera camera in currentCutScene.cameras)
                {
                    if (currentCamera == camera)
                    {
                        currentIndex = i;
                    }
                    i++;

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Toggle(camera == currentCamera, camera.FullDescription(), toggleStyle, GUILayout.Width(LIST_WIDTH - 36)))
                    {
                        currentCamera = camera;
                        currentItem = camera;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            // Display the listing of actors
            else if (currentMode == Modes.Actor)
            {
                listCount = currentCutScene.actors.Count;
                foreach (Actor actor in currentCutScene.actors)
                {
                    if (currentActor == actor)
                    {
                        currentIndex = i;
                    }
                    i++;

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Toggle(actor == currentActor, actor.FullDescription(), toggleStyle, GUILayout.Width(LIST_WIDTH - 36)))
                    {
                        currentActor = actor;
                        currentItem = actor;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            // Display the listing of cut scene actions
            else if (currentMode == Modes.Action)
            {
                listCount = currentCutScene.actions.Count;
                foreach (CutSceneAction action in currentCutScene.actions)
                {
                    if (currentAction == action)
                    {
                        currentIndex = i;
                    }
                    i++;

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Toggle(action == currentAction, action.FullDescription(), toggleStyle, GUILayout.Width(LIST_WIDTH - 72)))
                    {
                        currentAction = action;
                        currentItem = action;
                    }
                    GUILayout.BeginVertical();
                    GUILayout.Space(3);
                    action.async = GUILayout.Toggle(action.async, new GUIContent("", "If checked, moves to the next action before waiting for this one to complete."));
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Button("New");
            if (GUILayout.Button("Up") && currentIndex > 0)
            {
                if (currentMode == Modes.Actor)
                {
                    currentCutScene.actors[currentIndex] = currentCutScene.actors[currentIndex - 1];
                    currentCutScene.actors[currentIndex - 1] = currentActor;
                }
                else if (currentMode == Modes.Camera)
                {
                    currentCutScene.cameras[currentIndex] = currentCutScene.cameras[currentIndex - 1];
                    currentCutScene.cameras[currentIndex - 1] = currentCamera;
                }
                else
                {
                    currentCutScene.actions[currentIndex] = currentCutScene.actions[currentIndex - 1];
                    currentCutScene.actions[currentIndex - 1] = currentAction;
                }
            }
            if (GUILayout.Button("Down") && currentIndex != -1 && currentIndex != listCount - 1)
            {
                if (currentMode == Modes.Actor)
                {
                    currentCutScene.actors[currentIndex] = currentCutScene.actors[currentIndex + 1];
                    currentCutScene.actors[currentIndex + 1] = currentActor;
                }
                else if (currentMode == Modes.Camera)
                {
                    currentCutScene.cameras[currentIndex] = currentCutScene.cameras[currentIndex + 1];
                    currentCutScene.cameras[currentIndex + 1] = currentCamera;
                }
                else
                {
                    currentCutScene.actions[currentIndex] = currentCutScene.actions[currentIndex + 1];
                    currentCutScene.actions[currentIndex + 1] = currentAction;
                }
            }
            if (GUILayout.Button("Delete") && currentIndex != -1)
            {
                deleteCurrent = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            DrawDetailPane();

            GUILayout.EndHorizontal();

            GUI.DragWindow();

            if (Event.current.type == EventType.Repaint && GUI.tooltip != toolTip)
            {
                toolTipTime = Time.fixedTime;
                toolTip = GUI.tooltip;
            }
        }

        private void DrawDetailPane()
        {
            const int DETAIL_WIDTH = 500;
            GUILayout.BeginVertical(GUILayout.Width(DETAIL_WIDTH));
            detailScrollPosition = GUILayout.BeginScrollView(detailScrollPosition, false, true, GUILayout.Height(LIST_HEIGHT + 64 ));

            if (currentItem != null)
            {
                currentItem.Draw();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DeleteGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Delete " + currentMode + " '" + currentItem.FullDescription() + "'?");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Yes"))
            {
                deleteCurrent = false;
                if (currentMode == Modes.Actor)
                {
                    currentCutScene.actors.Remove(currentActor);
                    currentActor = null;
                    currentItem = null;
                }
                else if (currentMode == Modes.Camera)
                {
                    currentCutScene.cameras.Remove(currentCamera);
                    currentCamera = null;
                    currentItem = null;
                }
                else
                {
                    currentCutScene.actions.Remove(currentAction);
                    currentAction = null;
                    currentItem = null;
                }
            }
            if (GUILayout.Button("No"))
            {
                deleteCurrent = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUI.DragWindow();
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
