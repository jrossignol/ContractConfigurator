using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /*
     * ParameterFactory wrapper for ReachBiome ContractParameter.
     */
    [Obsolete("Obsolete, use ReachState")]
    public class ReachBiomeFactory : ParameterFactory
    {
        protected string biome;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get biome
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "biome", ref biome, this);

            LoggingUtil.LogError(this, "ReachBiome is obsolete as of ContractConfigurator 0.5.3, please use ReachState instead.  ReachBiome will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachBiomeCustom(biome, title);
        }
    }
}
