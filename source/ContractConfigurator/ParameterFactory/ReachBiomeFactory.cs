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
     * ParameterFactory wrapper for ReachBiome ContractParameter.
     */
    public class ReachBiomeFactory : ParameterFactory
    {
        protected string biome { get; set; }
        protected string title { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get biome
            if (!configNode.HasValue("biome"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'biome'.");
            }
            biome = configNode.GetValue("biome");

            // Get title
            title = configNode.HasValue("title") ? configNode.GetValue("title") : "Biome: ";

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachBiome(biome, title);
        }
    }
}
