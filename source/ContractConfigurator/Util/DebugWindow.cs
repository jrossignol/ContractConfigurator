using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Contracts;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    public static class DebugWindow
    {
        private enum SelectedPane
        {
            DEBUG_LOG,
            BALANCE_MODE,
            LOCATION_MODE,
        }
        private static SelectedPane selectedPane = SelectedPane.DEBUG_LOG;

        private static Rect windowPos = new Rect(580f, 200f, 1f, 1f);
        public static Vector2 scrollPosition, scrollPosition2;
        private static IEnumerable<ContractType> guiContracts;

        private static Texture2D closeIcon;
        private static Texture2D check;
        private static Texture2D cross;
        private static GUIStyle greenLabel;
        private static GUIStyle redLabel;
        private static GUIStyle yellowLabel;
        private static GUIStyle greenLegend;
        private static GUIStyle redLegend;
        private static GUIStyle yellowLegend;
        private static GUIStyle selectedButton;
        private static GUIStyle clippedLabel;
        private static GUIStyle clippedLabelRight;
        private static GUIStyle headerLabel;
        private static GUIStyle headerLabelCenter;
        private static GUIStyle headerLabelRight;
        private static GUIStyle bigTipStyle;
        private static GUIStyle tipStyle;

        private static Rect tooltipPosition;
        private static string tooltip = "";
        private static double toolTipTime = 0.0;
        private static bool drawToolTip = false;

        public static bool showGUI = false;

        public static void LoadTextures()
        {
            check = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/check", false);
            cross = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/cross", false);
            closeIcon = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/close", false);
        }

        public static void OnGUI()
        {
            if (showGUI && HighLogic.LoadedScene != GameScenes.CREDITS && HighLogic.LoadedScene != GameScenes.LOADING &&
                HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.SETTINGS)
            {
                var ainfoV = Attribute.GetCustomAttribute(typeof(ContractConfigurator).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                windowPos = GUILayout.Window(
                    typeof(ContractConfigurator).FullName.GetHashCode(),
                    windowPos,
                    WindowGUI,
                    "Contract Configurator " + ainfoV.InformationalVersion);

                // Add the close icon
                if (GUI.Button(new Rect(windowPos.xMax - 18, windowPos.yMin + 2, 16, 16), closeIcon, GUI.skin.label))
                {
                    showGUI = false;
                }

                GUI.depth = 0;
                if (drawToolTip)
                {
                    DrawToolTip();
                }
            }
        }

        private static void WindowGUI(int windowID)
        {
            // Set up labels
            if (greenLabel == null)
            {
                greenLabel = new GUIStyle(GUI.skin.label);
                greenLabel.normal.textColor = Color.green;
                redLabel = new GUIStyle(GUI.skin.label);
                redLabel.normal.textColor = Color.red;
                yellowLabel = new GUIStyle(GUI.skin.label);
                yellowLabel.normal.textColor = Color.yellow;
                greenLegend = new GUIStyle(redLabel);
                greenLegend.alignment = TextAnchor.UpperCenter;
                redLegend = new GUIStyle(redLabel);
                redLegend.alignment = TextAnchor.UpperCenter;
                yellowLegend = new GUIStyle(yellowLabel);
                yellowLegend.alignment = TextAnchor.UpperCenter;

                selectedButton = new GUIStyle(GUI.skin.button);
                selectedButton.normal.textColor = new Color(1.0f, 0.65f, 0f);

                clippedLabel = new GUIStyle(GUI.skin.label);
                clippedLabel.clipping = TextClipping.Clip;
                clippedLabel.wordWrap = false;
                clippedLabel.richText = true;

                clippedLabelRight = new GUIStyle(clippedLabel);
                clippedLabelRight.alignment = TextAnchor.UpperRight;

                headerLabel = new GUIStyle(GUI.skin.label);
                headerLabel.fontStyle = FontStyle.Bold;
                headerLabel.richText = true;
                headerLabel.padding = new RectOffset(0, 0, 0, 0);

                headerLabelCenter = new GUIStyle(headerLabel);
                headerLabelCenter.alignment = TextAnchor.UpperCenter;

                headerLabelRight = new GUIStyle(headerLabel);
                headerLabelRight.alignment = TextAnchor.UpperRight;

                bigTipStyle = new GUIStyle(GUI.skin.label);
                bigTipStyle.richText = true;

                tipStyle = new GUIStyle(GUI.skin.box);
                tipStyle.wordWrap = true;
                tipStyle.stretchHeight = true;
                tipStyle.normal.textColor = Color.white;
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(500));

            string loadedText = ContractConfigurator.reloading ? "Reloading contracts..." :
                "Sucessfully loaded " + ContractConfigurator.successContracts + " out of " +
                ContractConfigurator.totalContracts + " contracts.";
            GUILayout.Label(loadedText, ContractConfigurator.reloading ? yellowLabel :
                ContractConfigurator.successContracts == ContractConfigurator.totalContracts ? greenLabel : redLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Contracts"))
            {
                ContractConfigurator.StartReload();
            }
            if (HighLogic.LoadedScene != GameScenes.MAINMENU)
            {
                if (GUILayout.Button("Force Check Requirements"))
                {
                    CheckRequirements();
                }
            }
            GUILayout.EndHorizontal();

            // Display the listing of contracts
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(640));
            if (Event.current.type == EventType.layout)
            {
                guiContracts = ContractType.AllContractTypes;
            }

            if (!ContractConfigurator.reloading)
            {
                foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g == null || g.parent == null))
                {
                    GroupGui(contractGroup, 0);
                }
            }

            GUILayout.EndScrollView();

            // Display the legend
            GUILayout.Label("Legend:");
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Met Requirement", greenLabel, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Unmet Requirement / Warnings", yellowLegend, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Disabled Item", redLegend, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(4);

            RightColumnGUI();

            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private static void GroupGui(ContractGroup contractGroup, int indent)
        {
            if (contractGroup != null)
            {
                GUILayout.BeginHorizontal();

                if (indent != 0)
                {
                    GUILayout.Label("", GUILayout.ExpandWidth(false), GUILayout.Width(indent * 16));
                }

                if (GUILayout.Button(contractGroup.expandInDebug ? "-" : "+", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    contractGroup.expandInDebug = !contractGroup.expandInDebug;
                }

                GUIStyle style = greenLabel;
                if (!contractGroup.enabled)
                {
                    style = redLabel;
                }
                else
                {
                    if (contractGroup.hasWarnings)
                    {
                        style = yellowLabel;
                    }

                    foreach (ContractType contractType in guiContracts.Where(ct => ct.group == contractGroup))
                    {
                        if (!contractType.enabled)
                        {
                            style = redLabel;
                            break;
                        }
                        else if (contractType.hasWarnings)
                        {
                            style = yellowLabel;
                        }
                    }
                }

                GUILayout.Label(new GUIContent(contractGroup.ToString(), DebugInfo(contractGroup)), style);
                GUILayout.EndHorizontal();
            }

            if (contractGroup == null || contractGroup.expandInDebug)
            {
                // Child groups
                if (contractGroup != null)
                {
                    foreach (ContractGroup childGroup in ContractGroup.AllGroups.Where(g => g != null && g.parent != null && g.parent.name == contractGroup.name))
                    {
                        GroupGui(childGroup, indent + 1);
                    }
                }

                // Child contract types
                foreach (ContractType contractType in guiContracts.Where(ct => ct.group == contractGroup))
                {
                    GUILayout.BeginHorizontal();

                    if (contractGroup != null)
                    {
                        GUILayout.Label("", GUILayout.ExpandWidth(false), GUILayout.Width((indent+1) * 16));
                    }

                    if (GUILayout.Button(contractType.expandInDebug ? "-" : "+", GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        contractType.expandInDebug = !contractType.expandInDebug;
                    }
                    GUILayout.Label(new GUIContent(contractType.ToString(), DebugInfo(contractType)),
                        contractType.enabled ? contractType.hasWarnings ? yellowLabel : GUI.skin.label : redLabel);
                    GUILayout.EndHorizontal();

                    if (contractType.expandInDebug)
                    {
                        // Output children
                        ParamGui(contractType, contractType.ParamFactories, indent + (contractGroup == null ? 1 : 2));
                        RequirementGui(contractType, contractType.Requirements, indent + (contractGroup == null ? 1 : 2));
                        BehaviourGui(contractType, contractType.BehaviourFactories, indent + (contractGroup == null ? 1 : 2));

                        GUILayout.Space(8);
                    }
                }
            }
        }


        private static void ParamGui(ContractType contractType, IEnumerable<ParameterFactory> paramList, int indent)
        {
            foreach (ParameterFactory param in paramList)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(28);
                GUILayout.Label(new GUIContent(new string(' ', indent * 4) + param, DebugInfo(param)),
                    param.enabled ? param.hasWarnings ? yellowLabel : GUI.skin.label : redLabel);
                if (contractType.enabled)
                {
                    if (GUILayout.Button(param.enabled ? check : cross, GUILayout.ExpandWidth(false)))
                    {
                        param.enabled = !param.enabled;
                    }
                }
                GUILayout.EndHorizontal();

                ParamGui(contractType, param.ChildParameters, indent + 1);
                RequirementGui(contractType, param.ChildRequirements, indent + 1);
            }
        }

        private static void RequirementGui(ContractType contractType, IEnumerable<ContractRequirement> requirementList, int indent)
        {
            foreach (ContractRequirement requirement in requirementList)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(28);
                GUIStyle style = requirement.lastResult == null ? GUI.skin.label : requirement.lastResult.Value ? greenLabel : yellowLabel;
                GUILayout.Label(new GUIContent(new string(' ', indent * 4) + requirement, DebugInfo(requirement)),
                    requirement.enabled ? requirement.hasWarnings ? yellowLabel : style : redLabel);
                if (contractType.enabled)
                {
                    if (GUILayout.Button(requirement.enabled ? check : cross, GUILayout.ExpandWidth(false)))
                    {
                        requirement.enabled = !requirement.enabled;
                    }
                }
                GUILayout.EndHorizontal();

                RequirementGui(contractType, requirement.ChildRequirements, indent + 1);
            }
        }

        private static void BehaviourGui(ContractType contractType, IEnumerable<BehaviourFactory> behaviourList, int indent)
        {
            foreach (BehaviourFactory behaviour in behaviourList)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(28);
                GUILayout.Label(new GUIContent(new string(' ', indent * 4) + behaviour, DebugInfo(behaviour)),
                    behaviour.enabled ? behaviour.hasWarnings ? yellowLabel : GUI.skin.label : redLabel);
                if (contractType.enabled)
                {
                    if (GUILayout.Button(behaviour.enabled ? check : cross, GUILayout.ExpandWidth(false)))
                    {
                        behaviour.enabled = !behaviour.enabled;
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void RightColumnGUI()
        {
            // The right column
            GUILayout.BeginVertical(GUILayout.Width(550));

            // Selector buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Debug Log", selectedPane == SelectedPane.DEBUG_LOG ? selectedButton : GUI.skin.button))
            {
                selectedPane = SelectedPane.DEBUG_LOG;
            }
            if (GUILayout.Button("Contract Balancing", selectedPane == SelectedPane.BALANCE_MODE ? selectedButton : GUI.skin.button))
            {
                selectedPane = SelectedPane.BALANCE_MODE;
            }
            if (FlightGlobals.ActiveVessel != null)
            {
                if (GUILayout.Button("Location", selectedPane == SelectedPane.LOCATION_MODE ? selectedButton : GUI.skin.button))
                {
                    selectedPane = SelectedPane.LOCATION_MODE;
                }
            }
            else if (selectedPane == SelectedPane.LOCATION_MODE)
            {
                selectedPane = SelectedPane.DEBUG_LOG;
            }
            GUILayout.EndHorizontal();

            if (selectedPane == SelectedPane.DEBUG_LOG)
            {
                drawToolTip = false;

                scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, GUILayout.Width(550), GUILayout.ExpandHeight(true));
                // Tooltip
                if (!string.IsNullOrEmpty(GUI.tooltip))
                {
                    tooltip = GUI.tooltip.Replace("\t", "    ");
                }
                GUILayout.Label(tooltip, bigTipStyle);
                GUILayout.EndScrollView();
            }
            else if (selectedPane == SelectedPane.BALANCE_MODE)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    drawToolTip = string.IsNullOrEmpty(GUI.tooltip);
                }

                BalanceModeGUI();
            }
            else if (selectedPane == SelectedPane.LOCATION_MODE)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    drawToolTip = string.IsNullOrEmpty(GUI.tooltip);
                }

                LocationModeGUI();
            }

            GUILayout.EndVertical();
        }

        private static void BalanceModeGUI()
        {
            int CT_WIDTH = 164;
            int CB_WIDTH = 56;
            int FUNDS_WIDTH = 60;
            int FUNDS2_WIDTH = 48;
            int SCIENCE_WIDTH = 36;
            int REP_WIDTH = 34;

            GUILayout.BeginHorizontal();
            GUILayout.Label("", headerLabelCenter, GUILayout.Width(CT_WIDTH));
            GUILayout.Label("", headerLabelCenter, GUILayout.Width(CB_WIDTH));
            GUILayout.Label("", headerLabelCenter, GUILayout.Width(1));
            GUILayout.Label("<color=#8bed8b>Rewards</color>", headerLabelCenter, GUILayout.Width(FUNDS_WIDTH + SCIENCE_WIDTH + REP_WIDTH - 2));
            GUILayout.Label("", headerLabelCenter, GUILayout.Width(1));
            GUILayout.Label("<color=#eded8b>Adv</color>", headerLabelCenter, GUILayout.Width(FUNDS2_WIDTH));
            GUILayout.Label("", headerLabelCenter, GUILayout.Width(1));
            GUILayout.Label("<color=#ed0b0b>Failure</color>", headerLabelCenter, GUILayout.Width(FUNDS2_WIDTH + REP_WIDTH - 1));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Contract Type", headerLabel, GUILayout.Width(CT_WIDTH));
            GUILayout.Label("Body", headerLabel, GUILayout.Width(CB_WIDTH));
            GUILayout.Label("<color=#8bed8b>Funds</color>", headerLabelRight, GUILayout.Width(FUNDS_WIDTH));
            GUILayout.Label("<color=#8bed8b>Sci</color>", headerLabelRight, GUILayout.Width(SCIENCE_WIDTH));
            GUILayout.Label("<color=#8bed8b>Rep</color>", headerLabelRight, GUILayout.Width(REP_WIDTH));
            GUILayout.Label("<color=#eded8b>Funds</color>", headerLabelRight, GUILayout.Width(FUNDS2_WIDTH));
            GUILayout.Label("<color=#ed0b0b>Funds</color>", headerLabelRight, GUILayout.Width(FUNDS2_WIDTH));
            GUILayout.Label("<color=#ed0b0b>Rep</color>", headerLabelRight, GUILayout.Width(REP_WIDTH));
            GUILayout.EndHorizontal();

            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, GUILayout.Width(550), GUILayout.ExpandHeight(true));
            foreach (ContractType contractType in guiContracts)
            {
                CelestialBody body = contractType.targetBody;

                GUILayout.BeginHorizontal();

                GUILayout.Label(new GUIContent(contractType.name, contractType.title), clippedLabel, GUILayout.Width(CT_WIDTH));
                GUILayout.Label(new GUIContent(body != null ? body.name : "none", BodyMultiplier(body)), clippedLabel, GUILayout.Width(CB_WIDTH));
                GUILayout.Label(CurrencyGUIContent(Currency.Funds, contractType, contractType.rewardFunds), clippedLabelRight, GUILayout.Width(FUNDS_WIDTH));
                GUILayout.Label(CurrencyGUIContent(Currency.Science, contractType, contractType.rewardScience), clippedLabelRight, GUILayout.Width(SCIENCE_WIDTH));
                GUILayout.Label(CurrencyGUIContent(Currency.Reputation, contractType, contractType.rewardReputation), clippedLabelRight, GUILayout.Width(REP_WIDTH));
                GUILayout.Label(CurrencyGUIContent(Currency.Funds, contractType, contractType.advanceFunds), clippedLabelRight, GUILayout.Width(FUNDS2_WIDTH));
                GUILayout.Label(CurrencyGUIContent(Currency.Funds, contractType, contractType.failureFunds), clippedLabelRight, GUILayout.Width(FUNDS2_WIDTH));
                GUILayout.Label(CurrencyGUIContent(Currency.Reputation, contractType, contractType.failureReputation), clippedLabelRight, GUILayout.Width(REP_WIDTH));

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
            {
                tooltip = GUI.tooltip;
            }
        }

        static GUIContent CurrencyGUIContent(Currency currency, ContractType contractType, double baseValue)
        {
            // Figure out the multiplier
            double multiplier = 1.0;
            if (GameVariables.Instance != null && contractType.targetBody != null)
            {
                multiplier *= GameVariables.Instance.GetContractDestinationWeight(contractType.targetBody);
            }
            string multInfo = baseValue.ToString("N0") + " (base) * " + multiplier.ToString("F1") + " (body)";
            if (GameVariables.Instance != null && contractType.prestige.Count > 0)
            {
                double val = GameVariables.Instance.GetContractPrestigeFactor(contractType.prestige.First());
                multiplier *= val;
                multInfo += " * " + val.ToString("F2") + " (prestige)";
            }
            
            // Get the proper amount, add color
            double adjustedValue = multiplier * baseValue;
            string text = "<color=#";
            switch (currency)
            {
                case Currency.Funds:
                    text += "b4d455";
                    break;
                case Currency.Reputation:
                    text += "e0d503";
                    break;
                case Currency.Science:
                    text += "6dcff6";
                    break;
            }
            text += ">" + adjustedValue.ToString("N0")  + "</color>";

            // Return the gui content
            return new GUIContent(text, multInfo);
        }

        static string BodyMultiplier(CelestialBody body)
        {
            string output = "Multiplier: ";
            if (body == null)
            {
                output += "N/A";
            }
            else if (GameVariables.Instance == null)
            {
                output += "Game not initialized.";
            }
            else
            {
                output += GameVariables.Instance.GetContractDestinationWeight(body).ToString("N1");
            }
            return output;
        }

        /// <summary>
        /// Draw tool tips.
        /// </summary>
        private static void DrawToolTip()
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                if (Time.fixedTime > toolTipTime + 0.5)
                {
                    GUIContent tip = new GUIContent(tooltip);

                    Vector2 textDimensions = GUI.skin.box.CalcSize(tip);
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

        /// <summary>
        /// Does a forced check of all contract requirements
        /// </summary>
        static void CheckRequirements()
        {
            foreach (ContractType contractType in ContractType.AllValidContractTypes)
            {
                foreach (ContractRequirement requirement in contractType.Requirements)
                {
                    CheckRequirement(requirement);
                }
            }
        }

        /// <summary>
        /// Forced check of contract requirement and its children.
        /// </summary>
        static void CheckRequirement(ContractRequirement requirement)
        {
            try
            {
                requirement.lastResult = requirement.RequirementMet(null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            foreach (ContractRequirement child in requirement.ChildRequirements)
            {
                CheckRequirement(child);
            }
        }

        private static Dictionary<IContractConfiguratorFactory, KeyValuePair<double, string>> toolTipCache =
            new Dictionary<IContractConfiguratorFactory, KeyValuePair<double, string>>();

        /// <summary>
        /// Outputs the debugging info for the debug window.
        /// </summary>
        static string DebugInfo(IContractConfiguratorFactory obj)
        {
            if (!toolTipCache.ContainsKey(obj) || toolTipCache[obj].Key != obj.dataNode.lastModified)
            {
                string result = "";
                result += "<b><color=white>Config Node Details</color></b>\n";
                result += obj.config;
                result += "\n\n";
                result += "<b><color=white>Config Details After Expressions</color></b>\n";
                result += DataNodeDebug(obj.dataNode, obj is BehaviourFactory) + "\n";
                result += "<b><color=white>Log Details</color></b>\n";
                result += obj.log;

                toolTipCache[obj] = new KeyValuePair<double,string>(obj.dataNode.lastModified, result);
            }

            return toolTipCache[obj].Value;
        }

        static string DataNodeDebug(DataNode node, bool recursive)
        {
            string result = node.DebugString() + "\n";
            if (recursive)
            {
                foreach (DataNode child in node.Children)
                {
                    result += DataNodeDebug(child, true);
                }
            }

            return result;
        }

        private static void LocationModeGUI()
        {
            int TOP_HEADER_WIDTH = 96;
            int TOP_EDIT_WIDTH = 160;
            int CITY_WIDTH = 124;
            int HEADING_WIDTH = 400;

            Vessel v = FlightGlobals.ActiveVessel;
            CelestialBody body = v.mainBody;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Latitude", headerLabel, GUILayout.Width(TOP_HEADER_WIDTH));
            GUILayout.TextField(v.latitude.ToString(), GUILayout.Width(TOP_EDIT_WIDTH));
            GUILayout.Label("Longitude", headerLabel, GUILayout.Width(TOP_HEADER_WIDTH));
            GUILayout.TextField(v.longitude.ToString(), GUILayout.Width(TOP_EDIT_WIDTH));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Height", headerLabel, GUILayout.Width(TOP_HEADER_WIDTH));
            double height = v.altitude - LocationUtil.TerrainHeight(v.latitude, v.longitude, body);
            GUILayout.TextField(height.ToString(), GUILayout.Width(TOP_EDIT_WIDTH));
            GUILayout.Label("Altitude", headerLabel, GUILayout.Width(TOP_HEADER_WIDTH));
            GUILayout.TextField(v.altitude.ToString(), GUILayout.Width(TOP_EDIT_WIDTH));
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label("PQS City", headerLabel, GUILayout.Width(CITY_WIDTH));
            GUILayout.Label("PQS Offset", headerLabel, GUILayout.Width(HEADING_WIDTH));
            GUILayout.EndHorizontal();

            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, GUILayout.Width(550), GUILayout.ExpandHeight(true));
            foreach (PQSCity city in body.GetComponentsInChildren<PQSCity>(true))
            {
                // Translate to a position in the PQSCity's coordinate system
                Vector3d vPos = FlightGlobals.ActiveVessel.transform.position - city.transform.position;
                Vector3d pos = new Vector3d(Vector3d.Dot(vPos, city.transform.right), Vector3d.Dot(vPos, city.transform.forward), Vector3d.Dot(vPos, city.transform.up));

                GUILayout.BeginHorizontal();

                GUILayout.Label(new GUIContent(city.name, city.name), clippedLabel, GUILayout.Width(CITY_WIDTH));
                GUILayout.TextField(pos.x.ToString() + ", " + pos.y.ToString() + ", " + pos.z.ToString(), GUILayout.Width(HEADING_WIDTH));
                
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
            {
                tooltip = GUI.tooltip;
            }
        }
    }
}
