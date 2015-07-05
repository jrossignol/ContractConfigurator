using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for changing vessel ownership.
    /// </summary>
    public class ChangeVesselOwnership : ContractBehaviour
    {
        public enum State
        {
            ContractAccepted,
            ContractCompletedFailure,
            ContractCompletedSuccess,
            ParameterCompleted,
        }

        private State onState;
        private List<string> vessels;
        private bool owned;
        private List<string> parameter;

        public ChangeVesselOwnership()
        {
        }

        public ChangeVesselOwnership(State onState, List<string> vessels, bool owned, List<string> parameter)
        {
            this.onState = onState;
            this.vessels = vessels;
            this.owned = owned;
            this.parameter = parameter;
        }

        private void SetVessels()
        {
            foreach (Vessel vessel in vessels.Select(v => ContractVesselTracker.Instance.GetAssociatedVessel(v)))
            {
                if (vessel != null)
                {
                    vessel.DiscoveryInfo.SetLevel(owned ? DiscoveryLevels.Owned : DiscoveryLevels.Unowned);
                }
            }
        }

        protected override void OnAccepted()
        {
            if (onState == State.ContractAccepted)
            {
                SetVessels();
            }
        }

        protected override void OnCancelled()
        {
            if (onState == State.ContractCompletedFailure)
            {
                SetVessels();
            }
        }

        protected override void OnDeadlineExpired()
        {
            if (onState == State.ContractCompletedFailure)
            {
                SetVessels();
            }
        }

        protected override void OnFailed()
        {
            if (onState == State.ContractCompletedFailure)
            {
                SetVessels();
            }
        }

        protected override void OnCompleted()
        {
            if (onState == State.ContractCompletedSuccess)
            {
                SetVessels();
            }
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            if (onState == State.ParameterCompleted && param.State == ParameterState.Complete && parameter.Contains(param.ID))
            {
                SetVessels();
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            onState = ConfigNodeUtil.ParseValue<State>(configNode, "onState");
            owned = ConfigNodeUtil.ParseValue<bool>(configNode, "owned");
            vessels = ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", new List<string>());
            parameter = ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", new List<string>());
        }

        protected override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("onState", onState);
            configNode.AddValue("owned", owned);
            foreach (string v in vessels)
            {
                configNode.AddValue("vessel", v);
            }
            foreach (string p in parameter)
            {
                configNode.AddValue("parameter", p);
            }
        }
    }
}
