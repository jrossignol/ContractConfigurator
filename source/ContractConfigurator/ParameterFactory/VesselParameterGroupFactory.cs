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
    /// <summary>
    /// ParameterFactory to provide logic for a parameter that groups vessel related parameters together.
    /// </summary>
    public class VesselParameterGroupFactory : ParameterFactory
    {
        protected double duration;
        protected string define;
        protected List<string> vesselList;

        public IEnumerable<string> Vessel { get { return vesselList; } }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get duration
            string durationStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "duration", ref durationStr, this, "");
            if (durationStr != null)
            {
                duration = durationStr != "" ? DurationUtil.ParseDuration(durationStr) : 0.0;
            }

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "define", ref define, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", ref vesselList, this, new List<string>());

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.VesselParameterGroup(title, define, vesselList, duration);
        }
    }
}
