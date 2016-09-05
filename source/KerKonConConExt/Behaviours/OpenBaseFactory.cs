using System;
using System.Collections.Generic;
using System.Linq;
using Contracts;
using KerbalKonstructs.LaunchSites;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;

namespace KerKonConConExt
{
	public class OpenBaseFactory : BehaviourFactory
	{
		protected List<OpenBase.ConditionDetail> conditions = new List<OpenBase.ConditionDetail>();
		protected string basename;

		public override bool Load(ConfigNode configNode)
		{
			// Load base class
			bool valid = base.Load(configNode);

			valid &= ConfigNodeUtil.ParseValue<string>(configNode, "basename", x => basename = x, this);

			int index = 0;
			foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, "CONDITION"))
			{
				DataNode childDataNode = new DataNode("CONDITION_" + index++, dataNode, this);
				try
				{
					ConfigNodeUtil.SetCurrentDataNode(childDataNode);
					OpenBase.ConditionDetail cd = new OpenBase.ConditionDetail();
					valid &= ConfigNodeUtil.ParseValue<OpenBase.ConditionDetail.Condition>(child, "condition", x => cd.condition = x, this);
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

		protected bool ValidateMandatoryParameter(string parameter, OpenBase.ConditionDetail.Condition condition)
		{
			if (parameter == null && (condition == OpenBase.ConditionDetail.Condition.PARAMETER_COMPLETED ||
				condition == OpenBase.ConditionDetail.Condition.PARAMETER_FAILED))
			{
				throw new ArgumentException("Required if condition is PARAMETER_COMPLETED or PARAMETER_FAILED.");
			}
			return true;
		}

		public override ContractBehaviour Generate(ConfiguredContract contract)
		{
			return new OpenBase(conditions, basename);
		}
	}
}