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
        protected string biome { get; set; }
        protected ExperimentSituations? situation { get; set; }
        protected BodyLocation? location { get; set; }
        protected string experiment { get; set; }
        protected CollectScienceCustom.RecoveryMethod recoveryMethod { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "biome", x => biome = x, this, "");
            valid &= ConfigNodeUtil.ParseValue<ExperimentSituations?>(configNode, "situation", x => situation = x, this, (ExperimentSituations?)null);
            valid &= ConfigNodeUtil.ParseValue<BodyLocation?>(configNode, "location", x => location = x, this, (BodyLocation?)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experiment", x => experiment = x, this, "", ValidateExperiment);
            valid &= ConfigNodeUtil.ParseValue<CollectScienceCustom.RecoveryMethod>(configNode, "recoveryMethod", x => recoveryMethod = x, this, CollectScienceCustom.RecoveryMethod.None);

            return valid;
        }

        protected bool ValidateExperiment(string experiment)
        {
            if (string.IsNullOrEmpty(experiment))
            {
                return true;
            }

            if (!ResearchAndDevelopment.GetExperimentIDs().Contains(experiment))
            {
                throw new ArgumentException("Not a valid experiment!");
            }
            return true;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new CollectScienceCustom(targetBody, biome, situation, location, experiment, recoveryMethod, title);
        }
    }
}
