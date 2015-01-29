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
    /// Special parameter wrapper which is always incomplete, and invisible.  Use it to wrap other 
    /// parameters that need to be hidden temporarily.
    /// </summary>
    public class InvisibleWrapper : Contracts.ContractParameter
    {
        protected string title { get; set; }

        public InvisibleWrapper()
            : base()
        {
        }

        protected override string GetTitle()
        {
            return "";
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }
    }
}
