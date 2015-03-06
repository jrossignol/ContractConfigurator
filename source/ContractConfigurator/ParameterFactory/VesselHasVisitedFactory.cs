using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for VesselHasVisited ContractParameter.
    /// </summary>
    public class VesselHasVisitedFactory : ParameterFactory
    {
        protected FlightLog.EntryType situation;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<FlightLog.EntryType>(configNode, "situation", x => situation = x, this);
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.VesselHasVisited(targetBody, situation, title);
        }
    }
}
