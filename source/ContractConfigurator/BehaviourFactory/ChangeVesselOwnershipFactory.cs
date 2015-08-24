using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for ChangeVesselOwnership ContractBehaviour.
    /// </summary>
    public class ChangeVesselOwnershipFactory : BehaviourFactory
    {
        private ChangeVesselOwnership.State onState;
        private List<string> vessels;
        private bool owned;
        private List<string> parameter;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<TriggeredBehaviour.State>(configNode, "onState", x => onState = x, this, TriggeredBehaviour.State.ParameterCompleted);
            valid &= ConfigNodeUtil.ParseValue<bool?>(configNode, "owned", x => owned = x.Value, this, (bool?)true);
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", x => vessels = x, this);
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this, new List<string>());

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new ChangeVesselOwnership(onState, vessels, owned, parameter);
        }
    }
}
