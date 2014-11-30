using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for OR ContractParameter.
     */
    public class AnyFactory : ParameterFactory
    {
        protected string title { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get title
            title = configNode.HasValue("title") ? configNode.GetValue("title") : "Complete any ONE of the following:";

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.Any(title);
        }
    }
}
