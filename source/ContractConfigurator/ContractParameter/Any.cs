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
            : base()
        {
            this.title = "Complete any ONE of the following";
        }

        public Any(string title)
            : base()
        {
            this.title = title;
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
                if (contractParameter.State == ParameterState.Complete)
                {
                    SetComplete();
                }
            }
        }
    }
}
