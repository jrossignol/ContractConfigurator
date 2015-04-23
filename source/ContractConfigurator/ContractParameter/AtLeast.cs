using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// ContractParameter that is successful when at least n child parameters are successful.
    /// </summary>
    public class AtLeast : ContractConfiguratorParameter
    {
        int count;

        public AtLeast()
            : base(null)
        {
        }

        public AtLeast(string title, int count)
            : base(title)
        {
            this.count = count;
        }

        protected override string GetTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = string.Format("Complete at least {0} of the following", count);

                if (state == ParameterState.Complete)
                {
                    int counter = 0;
                    foreach (ContractParameter child in this.GetChildren())
                    {
                        if (child.State == ParameterState.Complete)
                        {
                            output += (counter == 0 ? ": " : ", ") + child.Title;
                            if (counter++ >= count)
                            {
                                break;
                            }
                        }
                    }
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
                if (this.GetChildren().Where(p => p.State == ParameterState.Complete).Count() >= count)
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
