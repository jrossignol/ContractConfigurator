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
     * ContractParameter that is successful when all child parameters are successful.
     */
    public class All : Contracts.ContractParameter
    {
        protected string title { get; set; }

        public All()
            : this(null)
        {
        }

        public All(string title)
            : base()
        {
            this.title = title;
        }

        protected override string GetTitle()
        {
            string output = null;
            if (title == null)
            {
                output = "Complete ALL of the following: ";

                if (state == ParameterState.Complete)
                {
                    bool first = true;
                    foreach (ContractParameter child in this.GetChildren())
                    {
                        if (child.State == ParameterState.Complete)
                        {
                            if (!first)
                            {
                                output += ", ";
                            }
                            output += child.Title;
                            first = false;
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
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnAnyContractParameterChange));
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
                if (this.GetChildren().All(p => p.State == ParameterState.Complete))
                {
                    SetComplete();
                }
                else
                {
                    SetIncomplete();
                }
            }
        }

        protected override void OnParameterStateChange(ContractParameter contractParameter)
        {
            if (System.Object.ReferenceEquals(contractParameter.Parent, this))
            {
                if (AllChildParametersComplete())
                {
                    SetComplete();
                }
                else if (AnyChildParametersFailed())
                {
                    SetFailed();
                }
            }
        }
    }
}
