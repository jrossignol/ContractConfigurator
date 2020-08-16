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
    /// ContractParameter that is successful when all child parameters are completed in order.
    /// </summary>
    public class Sequence : ContractConfiguratorParameter
    {
        protected List<string> hiddenParameters;

        private bool firstRun = false;
        private bool failWhenCompleteOutOfOrder = false;

        public Sequence()
            : base(null)
        {
        }

        public Sequence(List<string> hiddenParameters, bool failWhenCompleteOutOfOrder, string title)
            : base(title ?? Localizer.GetStringByTag("#cc.param.Sequence"))
        {
            this.hiddenParameters = hiddenParameters;
            this.failWhenCompleteOutOfOrder = failWhenCompleteOutOfOrder;
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            foreach (string param in hiddenParameters)
            {
                node.AddValue("hiddenParameter", param);
            }
            node.AddValue("failWhenCompleteOutOfOrder", failWhenCompleteOutOfOrder);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            hiddenParameters = ConfigNodeUtil.ParseValue<List<string>>(node, "hiddenParameter", new List<string>());
            failWhenCompleteOutOfOrder = ConfigNodeUtil.ParseValue<bool?>(node, "failWhenCompleteOutOfOrder", (bool?)false).Value;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!firstRun)
            {
                SetupChildParameters();
                firstRun = true;
            }
        }


        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnParameterChange(Contract contract, ContractParameter contractParameter)
        {
            if (contract == Root)
            {
                SetupChildParameters();

                bool foundNotComplete = false;
                for (int i = 0; i < ParameterCount; i++)
                {
                    ContractParameter param = GetParameter(i);
                    // Found an incomplete parameter
                    if (param.State != ParameterState.Complete)
                    {
                        foundNotComplete = true;

                        // If it's failed, just straight up fail the parameter
                        if (param.State == ParameterState.Failed)
                        {
                            SetState(ParameterState.Failed);
                            return;
                        }
                    }
                    // We found a complete parameter after finding an incomplete one - failure condition
                    else if (foundNotComplete && failWhenCompleteOutOfOrder)
                    {
                        SetState(ParameterState.Failed);
                        return;
                    }
                }

                // Everything we found was complete
                if (!foundNotComplete)
                {
                    SetState(ParameterState.Complete);
                }
            }
        }

        void SetupChildParameters()
        {
            if (!hiddenParameters.Any())
            {
                return;
            }

            bool foundIncomplete = false;
            foreach (ContractParameter child in this.GetChildren())
            {
                ContractConfiguratorParameter param = child as ContractConfiguratorParameter;
                if (param != null)
                {
                    // Need to potentially hide
                    if (foundIncomplete)
                    {
                        if (hiddenParameters.Contains(param.ID) && !param.hidden)
                        {
                            param.hidden = true;
                            ContractConfigurator.OnParameterChange.Fire(Root, param);
                        }
                    }
                    // Need to potentially unhide
                    else
                    {
                        if (hiddenParameters.Contains(param.ID) && param.hidden)
                        {
                            param.hidden = false;
                            ContractConfigurator.OnParameterChange.Fire(Root, param);
                        }
                    }
                }

                // Check on the state
                if (child.State != ParameterState.Complete)
                {
                    foundIncomplete = true;
                }
            }
        }
    }
}
