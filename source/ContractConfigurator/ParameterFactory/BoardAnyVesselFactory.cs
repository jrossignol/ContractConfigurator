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
     * ParameterFactory wrapper for BoardAnyVessel ContractParameter.
     */
    public class BoardAnyVesselFactory : ParameterFactory
    {
        protected string kerbal { get; set; }

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
            if (title == null)
            {
                title = kerbal + ": Board a vessel";
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            BoardAnyVessel contractParam = new BoardAnyVessel(title);
            contractParam .AddKerbal(kerbal);
            return contractParam;
        }
    }
}
