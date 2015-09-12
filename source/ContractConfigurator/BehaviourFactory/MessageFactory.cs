using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;
namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for Message ContractBehaviour.
    /// </summary>
    public class MessageFactory : BehaviourFactory
    {
        protected List<Message.ConditionDetail> conditions = new List<Message.ConditionDetail>();
        protected string title;
        protected string message;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", x => title = x, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "message", x => message = x, this);

            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, "CONDITION"))
            {
                DataNode childDataNode = new DataNode("CONDITION_" + index++, dataNode, this);
                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(childDataNode);
                    Message.ConditionDetail cd = new Message.ConditionDetail();
                    valid &= ConfigNodeUtil.ParseValue<Message.ConditionDetail.Condition>(child, "condition", x => cd.condition = x, this);
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "parameter", x => cd.parameter = x, this, "", x => ValidateMandatoryParameter(x, cd.condition));
                    conditions.Add(cd);
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);
                }
            }
            valid &= ConfigNodeUtil.ValidateMandatoryChild(configNode, "CONDITION", this);

            return valid;
        }

        protected bool ValidateMandatoryParameter(string parameter, Message.ConditionDetail.Condition condition)
        {
            if (parameter == null && (condition == Message.ConditionDetail.Condition.PARAMETER_COMPLETED ||
                condition == Message.ConditionDetail.Condition.PARAMETER_FAILED))
            {
                throw new ArgumentException("Required if condition is PARAMETER_COMPLETED or PARAMETER_FAILED.");
            }
            return true;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new Message(conditions, title, message);
        }
    }
}
