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
        protected int minCrew { get; set; }
        protected int maxCrew { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

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
            return new HasCrew(title, minCrew, maxCrew);
        }
    }
}
