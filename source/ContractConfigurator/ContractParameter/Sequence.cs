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
     * ContractParameter that is successful when all child parameters are completed in order.
     */
    public class Sequence : Contracts.ContractParameter
    {
        protected string title { get; set; }

        public Sequence()
            : this(null)
        {
        }

        public Sequence(string title)
            : base()
        {
            this.title = title != null && title != "" ? title : "Complete the following in order";
        }

        protected override string GetTitle()
        {
            return title;
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

        protected override void OnParameterStateChange(ContractParameter contractParameter)
        {
            if (System.Object.ReferenceEquals(contractParameter.Parent, this))
            {
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
                            SetFailed();
                            return;
                        }
                    }
                    // We found a complete parameter after finding an incomplete one - failure condition
                    else if (foundNotComplete)
                    {
                        SetFailed();
                        return;
                    }
                }

                // Everything we found was complete
                if (!foundNotComplete)
                {
                    SetComplete();
                }
            }
        }
    }
}
