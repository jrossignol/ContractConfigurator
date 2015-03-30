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
    [Obsolete("VesselHasVisited is obsolete as of Contract Configurator 0.7.5 and will be removed in 1.0.0.  Please use Orbit or ReachState (with disableOnStateChange = true) instead.")]
    public class VesselHasVisitedFactory : ParameterFactory
    {
        protected FlightLog.EntryType situation;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<FlightLog.EntryType>(configNode, "situation", x => situation = x, this);
            valid &= ValidateTargetBody(configNode);

            LoggingUtil.LogWarning(this, "VesselHasVisited is obsolete as of Contract Configurator 0.7.5 and will be removed in 1.0.0.  Please use Orbit or ReachState (with disableOnStateChange = true) instead.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.VesselHasVisited(targetBody, situation, title);
        }
    }
}
