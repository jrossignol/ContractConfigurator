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
    /// ContractParameter that is successful when any child parameter is successful.
    /// </summary>
    public class Any : ContractConfiguratorParameter
    {
        public Any()
            : this(null)
        {
        }

        public Any(string title)
            : base(title)
        {
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                // "Complete any ONE of the following"
                if (state == ParameterState.Complete)
                {
                    output = StringBuilderCache.Format("{0}: {1}", Localizer.GetStringByTag("#cc.param.Any"),
                        LocalizationUtil.LocalizeList<ContractParameter>(LocalizationUtil.Conjunction.AND, this.GetChildren().Where(x => x.State == ParameterState.Complete), x => x.Title));
                }
                else
                {
                    output = Localizer.GetStringByTag("#cc.param.Any");
                }
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
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
                    SetState(ParameterState.Complete);
                }
                else
                {
                    SetState(ParameterState.Incomplete);
                }
            }
        }
    }
}
