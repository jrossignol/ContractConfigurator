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
        protected int index;
        protected double distance;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", ref index, this, 1, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "distance", ref distance, this, 0.0, x => Validation.GE(x, 0.0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            VisitWaypoint vw = new VisitWaypoint(index, distance, title);
            return vw.FetchWaypoint(contract) != null ? vw : null;
        }
    }
}
