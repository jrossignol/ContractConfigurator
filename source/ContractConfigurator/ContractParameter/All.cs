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
            : base()
        {
            this.title = "Complete ALL of the following";
        }

        public All(string title)
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
