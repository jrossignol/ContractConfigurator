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
    /// ContractParameter that inverts the state of its child parameter.
    /// </summary>
    public class Not : ContractConfiguratorParameter
    {
        public Not()
            : base(null)
        {
        }

        public Not(string title)
            : base(title)
        {
            disableOnStateChange = true;
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = Localizer.GetStringByTag("#cc.param.Not");
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
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
                if (this.GetChildren().Where(p => p.State == ParameterState.Complete).Any())
                {
                    SetState(ParameterState.Incomplete);
                }
                else
                {
                    SetState(ParameterState.Complete);
                }
            }
        }
    }
}
