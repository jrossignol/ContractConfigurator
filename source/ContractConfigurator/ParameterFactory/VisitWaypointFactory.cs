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
    /// <summary>
    /// ParameterFactory for VisitWaypoint.
    /// </summary>
    public class VisitWaypointFactory : ParameterFactory
    {
        protected int index;
        protected double distance;
        protected bool hideOnCompletion;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "distance", x => distance = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "hideOnCompletion", x => hideOnCompletion = x, this, true);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            VisitWaypoint vw = new VisitWaypoint(index, distance, hideOnCompletion, title);
            return vw.FetchWaypoint(contract) != null ? vw : null;
        }
    }
}
