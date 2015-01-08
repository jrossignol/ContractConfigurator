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
    /*
     * ContractParameter that is successful when any child parameter is successful.
     */
    public class Any : Contracts.ContractParameter
    {
        protected string title { get; set; }

        public Any()
            : this(null)
        {
        }

        public Any(string title)
            : base()
        {
            this.title = title;
        }

        protected override string GetTitle()
        {
            string output = null;
            if (title == null)
            {
                output = "Complete any ONE of the following";

                if (state == ParameterState.Complete)
                {
                    foreach (ContractParameter child in this.GetChildren())
                    {
                        if (child.State == ParameterState.Complete)
                        {
                            output += ": " + child.Title;
                            break;
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

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }
        
        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
        }

        protected override void OnRegister()
        {
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract,ContractParameter>.OnEvent(OnAnyContractParameterChange));
        }

        protected override void OnUnregister()
        {
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnAnyContractParameterChange));
        }

        protected void OnAnyContractParameterChange(Contract contract, ContractParameter contractParameter)
        {
            if (contract == Root)
            {
                LoggingUtil.LogVerbose(this, "OnAnyContractParameterChange");
                if (this.GetChildren().Where(p => p.State == ParameterState.Complete).Any())
                {
                    SetComplete();
                }
                else
                {
                    SetIncomplete();
                }
            }
        }
    }
}
