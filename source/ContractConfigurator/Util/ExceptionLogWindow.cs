using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator
{
    public static class ExceptionLogWindow
    {
        public enum ExceptionSituation
        {
            PARAMETER_SAVE,
            PARAMETER_LOAD,
            CONTRACT_SAVE,
            CONTRACT_LOAD,
            OTHER,
        }

        private static string situationString;
        private static string actionString;
        private static Exception displayedException = null;
        private static Rect windowPos = new Rect(Screen.width / 2.0f - 300.0f, Screen.height / 2.0f - 120.0f, 600f, 240f);
        private static Vector2 scrollPosition;

        public static void DisplayFatalException(ExceptionSituation situation, Exception e, params object[] args)
        {
            switch(situation)
            {
                case ExceptionSituation.PARAMETER_SAVE:
                    situationString = String.Format("while saving contract parameter '{1}' in contract '{0}'", args);
                    actionString = "The contract data was not correctly saved - reloading the save may result in further errors.  Best case - the contract in question is no longer valid.";
                    break;
                case ExceptionSituation.PARAMETER_LOAD:
                    situationString = String.Format("while loading contract parameter '{1}' in contract '{0}'", args);
                    actionString = "The contract data was not correctly loaded.  Avoid saving your game and backup your save file immediately if you wish to prevent contract loss!";
                    break;
                case ExceptionSituation.CONTRACT_SAVE:
                    situationString = String.Format("while saving contract contract '{0}'", args);
                    actionString = "The contract data was not correctly saved - reloading the save may result in further errors.  Best case - the contract in question is no longer valid.";
                    break;
                case ExceptionSituation.CONTRACT_LOAD:
                    situationString = String.Format("while loading contract contract '{0}'", args);
                    actionString = "The contract data was not correctly loaded.  Avoid saving your game and backup your save file immediately if you wish to prevent contract loss!";
                    break;
                default:
                    situationString = "while performing an unspecified operation";
                    actionString = null;
                    break;
            }
            displayedException = e;
        }

        public static void OnGUI()
        {
            if (displayedException != null)
            {
                var ainfoV = Attribute.GetCustomAttribute(typeof(ExceptionLogWindow).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                windowPos = GUILayout.Window(
                    typeof(ExceptionLogWindow).FullName.GetHashCode(),
                    windowPos,
                    WindowGUI,
                    "Contract Configurator " + ainfoV.InformationalVersion + " Exception");
            }
        }

        private static void WindowGUI(int windowID)
        {
            GUI.skin = HighLogic.Skin;

            string label = "An unexpected exception occurred!  Please copy the error message below, and either post it on the Contract Configurator thread on the KSP forums, or raise an issue on our GitHub tracker.";
            if (actionString != null)
            {
                label += "\n\n" + actionString;
            }
            string message = displayedException != null ? displayedException.ToString() : "";

            GUILayout.BeginVertical();

            GUILayout.Label(label);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(280));
            GUILayout.TextArea("Exception occured " + situationString + ":\n" + message, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            if (GUILayout.Button("OK"))
            {
                displayedException = null;
            }

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
