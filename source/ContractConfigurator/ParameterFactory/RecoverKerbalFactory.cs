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
     * ParameterFactory wrapper for RecoverKerbal ContractParameter.
     */
    public class RecoverKerbalFactory : ParameterFactory
    {
        protected string kerbal { get; set; }
        protected string title { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get Kerbal
            kerbal = "";
            if (!configNode.HasValue("kerbal"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'kerbal'.");
            }
            kerbal = configNode.GetValue("kerbal");

            // Get title
            title = configNode.HasValue("title") ? configNode.GetValue("title") : kerbal + ": Recovered";

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            RecoverKerbal contractParam = new RecoverKerbal(title);
            contractParam .AddKerbal(kerbal);
            return contractParam;
        }
    }
}
