using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// ContractParameter that fails if n or more child parameters are successful.
    /// </summary>
    public class AtMost : ContractConfiguratorParameter
    {
        int count;

        public AtMost()
            : base(null)
        {
        }

        public AtMost(string title, int count)
            : base(title)
        {
            this.count = count;
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                // "Allow no more than <<1>> of the following"
                if (state == ParameterState.Complete)
                {
                    output = StringBuilderCache.Format("{0}: {1}", Localizer.Format("#cc.param.AtMost", count),
                        LocalizationUtil.LocalizeList<ContractParameter>(LocalizationUtil.Conjunction.OR, this.GetChildren().Where(x => x.State == ParameterState.Complete), x => x.Title));
                }
                else
                {
                    output = Localizer.Format("#cc.param.AtMost", count);
                }
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("count", count);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            count = ConfigNodeUtil.ParseValue<int>(node, "count");
        }

        protected override void OnRegister()
        {
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnAnyContractParameterChange));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnAnyContractParameterChange));
        }

        protected override void OnUnregister()
        {
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnAnyContractParameterChange));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnAnyContractParameterChange));
        }

        protected void OnAnyContractParameterChange(Contract contract, ContractParameter contractParameter)
        {
            if (contract == Root)
            {
                LoggingUtil.LogVerbose(this, "OnAnyContractParameterChange");
                if (this.GetChildren().Where(p => p.State == ParameterState.Complete).Count() > count)
                {
                    SetState(ParameterState.Failed);
                }
                else
                {
                    SetState(ParameterState.Complete);
                }
            }
        }
    }
}
