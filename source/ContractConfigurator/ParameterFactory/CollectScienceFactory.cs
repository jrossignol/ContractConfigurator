using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for CollectScience ContractParameter.
    /// </summary>
    public class CollectScienceFactory : ParameterFactory
    {
        protected Biome biome { get; set; }
        protected ExperimentSituations? situation { get; set; }
        protected BodyLocation? location { get; set; }
        protected ScienceExperiment experiment { get; set; }
        protected CollectScienceCustom.RecoveryMethod recoveryMethod { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<Biome>(configNode, "biome", x => biome = x, this, (Biome)null);
            valid &= ConfigNodeUtil.ParseValue<ExperimentSituations?>(configNode, "situation", x => situation = x, this, (ExperimentSituations?)null);
            valid &= ConfigNodeUtil.ParseValue<BodyLocation?>(configNode, "location", x => location = x, this, (BodyLocation?)null);
            valid &= ConfigNodeUtil.ParseValue<ScienceExperiment>(configNode, "experiment", x => experiment = x, this, (ScienceExperiment)null);
            valid &= ConfigNodeUtil.ParseValue<CollectScienceCustom.RecoveryMethod>(configNode, "recoveryMethod", x => recoveryMethod = x, this, CollectScienceCustom.RecoveryMethod.None);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new CollectScienceCustom(targetBody, biome == null ? "" : biome.biome, situation, location,
                experiment == null ? "" : experiment.id, recoveryMethod, title);
        }
    }
}
