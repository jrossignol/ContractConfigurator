using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for displaying a message box on contract/parameter completion/failure.
    /// </summary>
    public class MessageBox : ContractBehaviour
    {
        public class GUI : MonoBehaviour
        {
            public static GUIStyle labelStyle;

            private Rect windowPos = new Rect(0, 0, 0, 0);
            private bool visible = false;
            private string message = "";
            private string title = "";

            void Start()
            {
                DontDestroyOnLoad(this);
            }

            void OnGUI()
            {
                if (visible)
                {
                    if (windowPos.width == 0 && windowPos.height == 0)
                    {
                        windowPos = new Rect(Screen.width / 2 - 140, Screen.height / 2 - 100, 280, 12);
                    }

                    UnityEngine.GUI.skin = HighLogic.Skin;
                    windowPos = GUILayout.Window(GetType().FullName.GetHashCode(),
                        windowPos, DrawMessageBox, title, GUILayout.Width(280));
                }
            }

            void DrawMessageBox(int windowID)
            {
                if (labelStyle == null)
                {
                    labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                    labelStyle.alignment = TextAnchor.UpperCenter;
                }
                
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(message);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("OK"))
                {
                    visible = false;
                    Destroy(this);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                UnityEngine.GUI.DragWindow();
            }

            public static void DisplayMessage(string title, string message)
            {
                GUI gui = MapView.MapCamera.gameObject.GetComponent<MessageBox.GUI>();
                if (gui == null)
                {
                    LoggingUtil.LogVerbose(typeof(MessageBox), "Adding MessageBox");
                    gui = MapView.MapCamera.gameObject.AddComponent<MessageBox.GUI>();
                }

                gui.Show(title, message);
            }

            private void Show(string title, string message)
            {
                visible = true;
                this.title = title;
                this.message = message;
            }
        }

        public class ConditionDetail
        {
            public enum Condition
            {
                CONTRACT_FAILED,
                CONTRACT_COMPLETED,
                PARAMETER_FAILED,
                PARAMETER_COMPLETED
            }

            public Condition condition;
            public string parameter;
            public bool disabled = false;
        }

        protected List<ConditionDetail> conditions = new List<ConditionDetail>();
        protected string title;
        protected string message;

        public MessageBox()
            : base()
        {
        }

        public MessageBox(List<ConditionDetail> conditions, string title, string message)
        {
            this.conditions = conditions;
            this.title = title;
            this.message = message;
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            ConditionDetail.Condition cond = param.State == ParameterState.Complete ?
                ConditionDetail.Condition.PARAMETER_COMPLETED :
                ConditionDetail.Condition.PARAMETER_FAILED;
            if (param.State == ParameterState.Incomplete)
            {
                return;
            }

            foreach (ConditionDetail cd in conditions.Where(cd => !cd.disabled && cd.condition == cond && cd.parameter == param.ID))
            {
                GUI.DisplayMessage(title, message);
                cd.disabled = true;
            }
        }

        protected override void OnCompleted()
        {
            foreach (ConditionDetail cd in conditions.Where(cd => !cd.disabled && cd.condition == ConditionDetail.Condition.CONTRACT_COMPLETED))
            {
                GUI.DisplayMessage(title, message);
                cd.disabled = true;
            }
        }

        protected override void OnFailed()
        {
            foreach (ConditionDetail cd in conditions.Where(cd => !cd.disabled && cd.condition == ConditionDetail.Condition.CONTRACT_FAILED))
            {
                GUI.DisplayMessage(title, message);
                cd.disabled = true;
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (ConditionDetail cd in conditions)
            {
                ConfigNode child = new ConfigNode("CONDITION");
                configNode.AddNode(child);

                child.AddValue("condition", cd.condition);
                if (!string.IsNullOrEmpty(cd.parameter))
                {
                    child.AddValue("parameter", cd.parameter);
                }
                child.AddValue("disabled", cd.disabled);
            }
            configNode.AddValue("title", title);
            configNode.AddValue("message", message);
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            foreach (ConfigNode child in configNode.GetNodes("CONDITION"))
            {
                ConditionDetail cd = new ConditionDetail();
                cd.condition = ConfigNodeUtil.ParseValue<ConditionDetail.Condition>(child, "condition");
                cd.parameter = ConfigNodeUtil.ParseValue<string>(child, "parameter", (string)null);
                cd.disabled = ConfigNodeUtil.ParseValue<bool>(child, "disabled");
                conditions.Add(cd);
            }
            message = ConfigNodeUtil.ParseValue<string>(configNode, "message");
            title = ConfigNodeUtil.ParseValue<string>(configNode, "title");
        }
    }
}
