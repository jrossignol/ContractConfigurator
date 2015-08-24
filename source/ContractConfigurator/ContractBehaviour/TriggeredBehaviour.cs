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
    /// Abstract behaviour with a trigger state.
    /// </summary>
    public abstract class TriggeredBehaviour : ContractBehaviour
    {
        public enum State
        {
            CONTRACT_ACCEPTED,
            CONTRACT_FAILED,
            CONTRACT_SUCCESS,
            CONTRACT_COMPLETED,
            PARAMETER_COMPLETED
        }

        public enum LegacyState
        {
            ContractAccepted,
            ContractCompletedFailure,
            ContractCompletedSuccess,
            ParameterCompleted,
        }

        private State onState;
        private List<string> parameter;

        public TriggeredBehaviour()
        {
        }

        public TriggeredBehaviour(State onState, List<string> parameter)
        {
            this.onState = onState;
            this.parameter = parameter;
        }

        protected override void OnAccepted()
        {
            if (onState == State.CONTRACT_ACCEPTED)
            {
                TriggerAction();
            }
        }

        protected override void OnCancelled()
        {
            if (onState == State.CONTRACT_FAILED || onState == State.CONTRACT_COMPLETED)
            {
                TriggerAction();
            }
        }

        protected override void OnDeadlineExpired()
        {
            if (onState == State.CONTRACT_FAILED || onState == State.CONTRACT_COMPLETED)
            {
                TriggerAction();
            }
        }

        protected override void OnFailed()
        {
            if (onState == State.CONTRACT_FAILED || onState == State.CONTRACT_COMPLETED)
            {
                TriggerAction();
            }
        }

        protected override void OnCompleted()
        {
            if (onState == State.CONTRACT_SUCCESS || onState == State.CONTRACT_COMPLETED)
            {
                TriggerAction();
            }
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            if (onState == State.PARAMETER_COMPLETED && param.State == ParameterState.Complete && parameter.Contains(param.ID))
            {
                TriggerAction();
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            try
            {
                onState = ConfigNodeUtil.ParseValue<State>(configNode, "onState");
            }
            catch (ArgumentException ae)
            {
                try
                {
                    LegacyState state = ConfigNodeUtil.ParseValue<LegacyState>(configNode, "onState");
                    switch (state)
                    {
                        case LegacyState.ContractAccepted:
                            onState = State.CONTRACT_ACCEPTED;
                            break;
                        case LegacyState.ContractCompletedFailure:
                            onState = State.CONTRACT_FAILED;
                            break;
                        case LegacyState.ContractCompletedSuccess:
                            onState = State.CONTRACT_SUCCESS;
                            break;
                        case LegacyState.ParameterCompleted:
                            onState = State.PARAMETER_COMPLETED;
                            break;
                    }
                }
                catch
                {
                    throw ae;
                }
            }
            parameter = ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", new List<string>());
        }

        protected override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("onState", onState);
            foreach (string p in parameter)
            {
                configNode.AddValue("parameter", p);
            }
        }

        /// <summary>
        /// Called when the action needs to be triggered.
        /// </summary>
        protected abstract void TriggerAction();
    }
}
