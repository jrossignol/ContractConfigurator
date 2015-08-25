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
        private List<string> parameter = new List<string>();

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            bool stateLoadValid = ConfigNodeUtil.ParseValue<TriggeredBehaviour.State>(configNode, "onState", x => onState = x, this, TriggeredBehaviour.State.PARAMETER_COMPLETED);
            if (!stateLoadValid)
            {
                LoggingUtil.LogWarning(this, "Warning, values for onState have changed - attempting to load using obsolete values.");
                valid &= ConfigNodeUtil.ParseValue<TriggeredBehaviour.LegacyState>(configNode, "onState", x =>
                {
                    switch (x)
                    {
                        case TriggeredBehaviour.LegacyState.ContractAccepted:
                            onState = TriggeredBehaviour.State.CONTRACT_ACCEPTED;
                            break;
                        case TriggeredBehaviour.LegacyState.ContractCompletedFailure:
                            onState = TriggeredBehaviour.State.CONTRACT_FAILED;
                            break;
                        case TriggeredBehaviour.LegacyState.ContractCompletedSuccess:
                            onState = TriggeredBehaviour.State.CONTRACT_SUCCESS;
                            break;
                        case TriggeredBehaviour.LegacyState.ParameterCompleted:
                            onState = TriggeredBehaviour.State.PARAMETER_COMPLETED;
                            break;
                    }
                }, this);
            }

            valid &= ConfigNodeUtil.ParseValue<bool?>(configNode, "owned", x => owned = x.Value, this, (bool?)true);
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", x => vessels = x, this);

            if (onState == TriggeredBehaviour.State.PARAMETER_COMPLETED || onState == TriggeredBehaviour.State.PARAMETER_FAILED)
            {
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this, new List<string>());
            }

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new ChangeVesselOwnership(onState, vessels, owned, parameter);
        }
    }
}
