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
    /// ContractParameter that is successful when all child parameters are completed in order.
    /// </summary>
    public class Sequence : ContractConfiguratorParameter
    {
        protected List<string> hiddenParameters;

        private bool paramRemoved = false;
        private bool firstRun = false;

        public Sequence()
            : base(null)
        {
        }

        public Sequence(List<string> hiddenParameters, string title)
            : base(title ?? "Complete the following in order")
        {
            this.hiddenParameters = hiddenParameters;
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
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            hiddenParameters = ConfigNodeUtil.ParseValue<List<string>>(node, "hiddenParameter", new List<string>());
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

        protected override void OnParameterStateChange(ContractParameter contractParameter)
        {
            if (System.Object.ReferenceEquals(contractParameter.Parent, this))
            {
                SetupChildParameters();

                bool foundNotComplete = false;
                for (int i = 0; i < ParameterCount; i++)
                {
                    ContractParameter param = GetParameter(i);
                    // Found an incomplete parameter - okay as long as we don't later find a completed
                    // one - which would be an out of order error.
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
                    else if (foundNotComplete)
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
            int i = 0;
            foreach (ContractParameter child in ChildParameterIterator())
            {
                ContractParameter param = child;
                InvisibleWrapper wrapper = null;

                // Already wrapped
                if (param.GetType() == typeof(InvisibleWrapper))
                {
                    wrapper = param as InvisibleWrapper;
                    param = wrapper.GetParameter(0);
                }

                // Need to potentially hide
                if (foundIncomplete)
                {
                    if (hiddenParameters.Contains(param.ID) && wrapper == null)
                    {
                        wrapper = new InvisibleWrapper();
                        wrapper.AddParameter(param);
                        RemoveParameter(i);
                        paramRemoved = true;
                        AddParameter(wrapper);
                    }
                }
                // Need to potentially unhide
                else
                {
                    if (hiddenParameters.Contains(param.ID) && wrapper != null)
                    {
                        RemoveParameter(i);
                        paramRemoved = true;
                        AddParameter(param);
                    }
                }

                // Check on the state
                if (param.State != ParameterState.Complete)
                {
                    foundIncomplete = true;
                }

                i++;
            }

            if (paramRemoved)
            {
                ContractConfigurator.OnParameterChange.Fire(Root, this);
            }
        }

        IEnumerable<ContractParameter> ChildParameterIterator()
        {
            paramRemoved = false;
            int startPoint = -1;
            for (int i = 0; i < ParameterCount; i++)
            {
                if (paramRemoved)
                {
                    startPoint = i;
                    break;
                }
                else
                {
                    yield return GetParameter(i);
                }
            }

            // Pull everything off and re-iterate
            if (paramRemoved && startPoint != -1)
            {
                List<ContractParameter> stack = new List<ContractParameter>();
                while (startPoint < ParameterCount)
                {
                    stack.Add(GetParameter(startPoint-1));
                    RemoveParameter(startPoint-1);
                }

                while (stack.Any())
                {
                    ContractParameter param = stack.First();
                    AddParameter(param);
                    stack.RemoveAt(0);
                    yield return param;
                }
            }
        }
    }
}
