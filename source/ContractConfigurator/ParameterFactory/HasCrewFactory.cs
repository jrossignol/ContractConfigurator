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
    /*
     * ParameterFactory wrapper for HasCrew ContractParameter.
     */
    public class HasCrewFactory : ParameterFactory
    {
        protected string trait { get; set; }
        protected int minExperience { get; set; }
        protected int maxExperience { get; set; }
        protected int minCrew { get; set; }
        protected int maxCrew { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get trait
            if (!configNode.HasValue("trait"))
            {
                trait = null;
            }
            else
            {
                try
                {
                    trait = configNode.GetValue("trait");
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                        ": error parsing trait: " + e.Message);
                }
            }

            // Get minimum experience level
            if (configNode.HasValue("minExperience"))
            {
                minExperience = Convert.ToInt32(configNode.GetValue("minExperience"));
            }
            else
            {
                minExperience = 0;
            }

            // Get maximum experience level
            if (configNode.HasValue("maxExperience"))
            {
                maxExperience = Convert.ToInt32(configNode.GetValue("maxExperience"));
            }
            else
            {
                maxExperience = 5;
            }

            // Get minCrew
            if (configNode.HasValue("minCrew"))
            {
                minCrew = Convert.ToInt32(configNode.GetValue("minCrew"));
            }
            else
            {
                minCrew = 1;
            }

            // Get maxCrew
            if (configNode.HasValue("maxCrew"))
            {
                maxCrew = Convert.ToInt32(configNode.GetValue("maxCrew"));
            }
            else
            {
                maxCrew = int.MaxValue;
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new HasCrew(title, trait, minCrew, maxCrew, minExperience, maxExperience);
        }
    }
}
