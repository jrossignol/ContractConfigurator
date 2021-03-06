﻿using System;
using System.Collections.Generic;
using System.Linq;
using Contracts;
using KerbalKonstructs.LaunchSites;
using ContractConfigurator;

namespace KerKonConConExt
{
    public class CloseBase : ContractBehaviour
    {
        public class ConditionDetail
        {
            public enum Condition
            {
                CONTRACT_ACCEPTED,
                CONTRACT_FAILED,
                CONTRACT_SUCCESS,
                CONTRACT_COMPLETED,
                PARAMETER_FAILED,
                PARAMETER_COMPLETED
            }

            public Condition condition;
            public string parameter;
            public bool disabled = false;
        }

        protected List<ConditionDetail> conditions = new List<ConditionDetail>();
        protected string basename;

        public CloseBase()
            : base()
        {
        }

        public CloseBase(List<ConditionDetail> conditions, string basename)
        {
            this.conditions = conditions;
            this.basename = basename;
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
            CloseABase(cond, param.ID, param.State == ParameterState.Complete ? basename : basename);
        }

        protected override void OnAccepted()
        {
            CloseABase(ConditionDetail.Condition.CONTRACT_ACCEPTED, basename);
        }

        protected override void OnCompleted()
        {
            CloseABase(ConditionDetail.Condition.CONTRACT_SUCCESS, basename);
            CloseABase(ConditionDetail.Condition.CONTRACT_COMPLETED, basename);
        }

        protected override void OnCancelled()
        {
            CloseABase(ConditionDetail.Condition.CONTRACT_FAILED, basename);
            CloseABase(ConditionDetail.Condition.CONTRACT_COMPLETED, basename);
        }

        protected override void OnDeadlineExpired()
        {
            CloseABase(ConditionDetail.Condition.CONTRACT_FAILED, basename);
            CloseABase(ConditionDetail.Condition.CONTRACT_COMPLETED, basename);
        }

        protected override void OnFailed()
        {
            CloseABase(ConditionDetail.Condition.CONTRACT_FAILED, basename);
            CloseABase(ConditionDetail.Condition.CONTRACT_COMPLETED, basename);
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
            basename = ConfigNodeUtil.ParseValue<string>(configNode, "basename");
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
            configNode.AddValue("basename", basename);
        }

        protected void CloseABase(ConditionDetail.Condition condition, string sbasename)
        {
            if (conditions.Any(c => c.condition == condition))
                LaunchSiteManager.setSiteOpenCloseState(sbasename, "Closed");
        }

        protected void CloseABase(ConditionDetail.Condition condition, string parameterID, string sbasename)
        {
            foreach (ConditionDetail cd in conditions.Where(cd => !cd.disabled && cd.condition == condition && cd.parameter == parameterID))
            {
                LaunchSiteManager.setSiteOpenCloseState(sbasename, "Closed");
                cd.disabled = true;
            }
        }
    }
}