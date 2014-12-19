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
    /*
     * ParameterFactory wrapper for VesselHasVisited ContractParameter.
     */
    public class VesselHasVisitedFactory : ParameterFactory
    {
        protected FlightLog.EntryType situation { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Validate target body
            if (targetBody == null)
            {
                valid = false;
                LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                    ": targetBody for ReachDestination must be specified.");
            }

            // Get returnFrom
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "situation", this);
            if (valid)
            {
                try
                {
                    string situationStr = configNode.GetValue("situation");
                    situation = (FlightLog.EntryType)Enum.Parse(typeof(FlightLog.EntryType), situationStr);
                }
                catch (Exception e)
                {
                    valid = false;
                    LoggingUtil.LogError(this.GetType(), ErrorPrefix(configNode) +
                        ": error parsing situation: " + e.Message);
                }

            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.VesselHasVisited(targetBody, situation, title);
        }
    }
}
