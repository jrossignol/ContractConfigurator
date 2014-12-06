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
     * ParameterFactory to provide logic for a parameter that groups vessel related parameters together.
     */
    public class VesselParameterGroupFactory : ParameterFactory
    {
        protected double duration { get; set; }
        protected string vesselName { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get duration
            duration = configNode.HasValue("duration") ? DurationUtil.ParseDuration(configNode, "duration") : 0.0;

            // Get vesselName
            vesselName = configNode.HasValue("vesselName") ? configNode.GetValue("vesselName") : null;

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.VesselParameterGroup(title, vesselName, duration);
        }
    }
}
