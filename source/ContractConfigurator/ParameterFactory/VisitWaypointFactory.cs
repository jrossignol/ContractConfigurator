using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator
{
    /*
     * ParameterFactory for VisitWaypoint.
     */
    public class VisitWaypointFactory : ParameterFactory
    {
        protected int index { get; set; }
        protected double distance { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get waypoint index
            index = configNode.HasValue("index") ? Convert.ToInt32(configNode.GetValue("index")) : 0;

            // Get the distance
            distance = configNode.HasValue("distance") ? Convert.ToDouble(configNode.GetValue("distance")) : 0.0;
            // 500 surface
            // 15000 orbit/air

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            VisitWaypoint vw = new VisitWaypoint(index, distance, title);
            return vw.FetchWaypoint(contract) != null ? vw : null;
        }
    }
}
