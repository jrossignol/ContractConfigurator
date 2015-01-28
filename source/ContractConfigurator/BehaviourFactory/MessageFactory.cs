using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

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

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "title", ref title, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "message", ref message, this);

            foreach (ConfigNode child in configNode.GetNodes("CONDITION"))
            {
                Message.ConditionDetail cd = new Message.ConditionDetail();
                valid &= ConfigNodeUtil.ParseValue<Message.ConditionDetail.Condition>(child, "condition", ref cd.condition, this);
                valid &= ConfigNodeUtil.ParseValue<string>(child, "parameter", ref cd.parameter, this, (string)null, x => ValidateMandatoryParameter(x, cd.condition));
                conditions.Add(cd);
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
