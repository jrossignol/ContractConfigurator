using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator
{
    public static class DebugWindow
    {
        private static Rect windowPos = new Rect(580f, 200f, 240f, 40f);
        public static Vector2 scrollPosition, scrollPosition2;
        private static string tooltip = "";
        private static IEnumerable<ContractType> guiContracts;

        public static Texture2D check;
        public static Texture2D cross;
        public static GUIStyle greenLabel;
        public static GUIStyle redLabel;
        public static GUIStyle yellowLabel;
        public static GUIStyle greenLegend;
        public static GUIStyle redLegend;
        public static GUIStyle yellowLegend;

        public static void LoadTextures()
        {
            check = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/check", false);
            cross = GameDatabase.Instance.GetTexture("ContractConfigurator/icons/cross", false);
        }

        public static void OnGUI()
        {
            if (HighLogic.LoadedScene != GameScenes.CREDITS && HighLogic.LoadedScene != GameScenes.LOADING &&
                HighLogic.LoadedScene != GameScenes.LOADINGBUFFER && HighLogic.LoadedScene != GameScenes.SETTINGS)
            {
                var ainfoV = Attribute.GetCustomAttribute(typeof(ContractConfigurator).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                windowPos = GUILayout.Window(
                    typeof(ContractConfigurator).FullName.GetHashCode(),
                    windowPos,
                    WindowGUI,
                    "Contract Configurator " + ainfoV.InformationalVersion);
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
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(500));

            GUILayout.Label("Sucessfully loaded " + ContractConfigurator.successContracts + " out of " +
                ContractConfigurator.totalContracts + " contracts.",
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

            foreach (ContractType contractType in guiContracts)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(contractType.expandInDebug ? "-" : "+", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    contractType.expandInDebug = !contractType.expandInDebug;
                }
                GUILayout.Label(new GUIContent(contractType.ToString(), contractType.config + "\n\n" + contractType.log),
                    contractType.enabled ? GUI.skin.label : redLabel);
                GUILayout.EndHorizontal();

                if (contractType.expandInDebug)
                {
                    // Output children
                    ParamGui(contractType, contractType.ParamFactories);
                    RequirementGui(contractType, contractType.Requirements);
                    BehaviourGui(contractType, contractType.BehaviourFactories);

                    GUILayout.Space(8);
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
            GUILayout.Label("Unmet Requirement", yellowLegend, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Disabled Item", redLegend, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // The right column
            GUILayout.BeginVertical();
            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, GUILayout.Width(500), GUILayout.ExpandHeight(true));

            // Tooltip
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                tooltip = GUI.tooltip;
            }
            GUILayout.Label(tooltip);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private static void ParamGui(ContractType contractType, IEnumerable<ParameterFactory> paramList, int indent = 1)
        {
            foreach (ParameterFactory param in paramList)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(28);
                GUILayout.Label(new GUIContent(new string('\t', indent) + param, param.config + "\n\n" + param.log),
                    param.enabled ? GUI.skin.label : redLabel);
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

        private static void RequirementGui(ContractType contractType, IEnumerable<ContractRequirement> requirementList, int indent = 1)
        {
            foreach (ContractRequirement requirement in requirementList)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(28);
                GUIStyle style = requirement.lastResult == null ? GUI.skin.label : requirement.lastResult.Value ? greenLabel : yellowLabel;
                GUILayout.Label(new GUIContent(new string('\t', indent) + requirement, requirement.config + "\n\n" + requirement.log),
                    requirement.enabled ? style : redLabel);
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

        private static void BehaviourGui(ContractType contractType, IEnumerable<BehaviourFactory> behaviourList, int indent = 1)
        {
            foreach (BehaviourFactory behaviour in behaviourList)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(28);
                GUILayout.Label(new GUIContent(new string('\t', indent) + behaviour, behaviour.config + "\n\n" + behaviour.log),
                    behaviour.enabled ? GUI.skin.label : redLabel);
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
    }
}
