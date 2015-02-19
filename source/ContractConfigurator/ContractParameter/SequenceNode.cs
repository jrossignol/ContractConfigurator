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
    /// ContractParameter for use iwth the Sequence parameter.  Does not complete unless all
    /// previous items in the sequence have completed.
    /// </summary>
    public class SequenceNode : ContractConfiguratorParameter
    {
        public SequenceNode()
            : this(null)
        {
        }

        public SequenceNode(string title)
            : base(title)
        {
        }

        protected override string GetTitle()
        {
            if (title != null && title != "")
            {
                return title;
            }
            else if (state == ParameterState.Complete)
            {
                if (ParameterCount == 1)
                {
                    return GetParameter(0).Title;
                }
                else
                {
                    return "Completed";
                }
            }
            else if (ReadyToComplete())
            {
                return "Complete the following";
            }
            else
            {
                return "Complete after the previous step";
            }
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

        /// <summary>
        /// Checks if this parameter is ready to complete - in other words, all previous parameters
        /// in the sequence have been completed.
        /// </summary>
        /// <returns>True if the parameter is ready to complete.</returns>
        private bool ReadyToComplete()
        {
            // Go through the parent's parameters
            for (int i = 0; i < Parent.ParameterCount; i++)
            {
                ContractParameter param = Parent.GetParameter(i);
                // If we've made it all the way to us, we're ready
                if (System.Object.ReferenceEquals(param, this))
                {
                    return true;
                }
                else if (param.State != ParameterState.Complete)
                {
                    return false;
                }
            }

            // Shouldn't get here unless things are really messed up
            LoggingUtil.LogWarning(this.GetType(), "Unexpected state for SequenceNode parameter.  Log a GitHub issue!");
            return false;
        }

        protected override void OnParameterStateChange(ContractParameter contractParameter)
        {
            if (System.Object.ReferenceEquals(contractParameter.Parent, this))
            {
                if (AllChildParametersComplete())
                {
                    if (ReadyToComplete())
                    {
                        SetState(ParameterState.Complete);
                    }
                    else
                    {
                        SetState(ParameterState.Incomplete);
                    }
                }
                else if (AnyChildParametersFailed())
                {
                    SetState(ParameterState.Failed);
                }
            }
        }
    }
}
