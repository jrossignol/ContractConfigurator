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
    public class Message : ContractBehaviour
    {
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

        public Message()
            : base()
        {
        }

        public Message(List<ConditionDetail> conditions, string title, string message)
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
                DisplayMessage(title, message);
                cd.disabled = true;
            }
        }

        protected override void OnCompleted()
        {
            foreach (ConditionDetail cd in conditions.Where(cd => !cd.disabled && cd.condition == ConditionDetail.Condition.CONTRACT_COMPLETED))
            {
                DisplayMessage(title, message);
                cd.disabled = true;
            }
        }

        protected override void OnFailed()
        {
            foreach (ConditionDetail cd in conditions.Where(cd => !cd.disabled && cd.condition == ConditionDetail.Condition.CONTRACT_FAILED))
            {
                DisplayMessage(title, message);
                cd.disabled = true;
            }
        }

        protected void DisplayMessage(string title, string message)
        {
            MessageSystem.Instance.AddMessage(new MessageSystem.Message(title, message,
                MessageSystemButton.MessageButtonColor.GREEN,
                MessageSystemButton.ButtonIcons.MESSAGE));
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
