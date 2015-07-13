using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Simple timer implementation.
    /// </summary>
    public class Timer : ContractConfiguratorParameter
    {
        public enum TimerType
        {
            CONTRACT_ACCEPTANCE,
            NEXT_LAUNCH,
            PARAMETER_COMPLETION
        }

        protected double duration = 0.0;
        protected double endTime = 0.0;
        protected TimerType timerType;
        protected string parameter = "";

        private double lastUpdate = 0.0;

        private TitleTracker titleTracker = new TitleTracker();

        public Timer()
            : base()
        {
        }

        public Timer(double duration, TimerType timerType, string parameter, bool failContract)
            : base("")
        {
            this.duration = duration;
            this.timerType = timerType;
            this.parameter = parameter;
            this.fakeFailures = !failContract;
            
            disableOnStateChange = false;
        }

        protected override string GetParameterTitle()
        {
            if (state == ParameterState.Failed)
            {
                return "Time expired!";
            }
            else if (endTime > 0.01)
            {
                string title = "Time remaining: " + DurationUtil.StringValue(endTime - Planetarium.GetUniversalTime());

                // Add the string that we returned to the titleTracker.  This is used to update
                // the contract title element in the GUI directly, as it does not support dynamic
                // text.
                titleTracker.Add(title);

                return title;
            }
            else
            {
                return "Time limit: " + DurationUtil.StringValue(duration);
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("duration", duration);
            node.AddValue("endTime", endTime);
            node.AddValue("timerType", timerType);
            if (!string.IsNullOrEmpty(parameter))
            {
                node.AddValue("parameter", parameter);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            duration = Convert.ToDouble(node.GetValue("duration"));
            endTime = Convert.ToDouble(node.GetValue("endTime"));
            timerType = ConfigNodeUtil.ParseValue<TimerType>(node, "timerType", TimerType.CONTRACT_ACCEPTANCE);
            parameter = ConfigNodeUtil.ParseValue<string>(node, "parameter", "");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
            GameEvents.onLaunch.Add(new EventData<EventReport>.OnEvent(OnLaunch));
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
            GameEvents.onLaunch.Remove(new EventData<EventReport>.OnEvent(OnLaunch));
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnContractAccepted(Contract contract)
        {
            // Set the end time
            if (contract == Root && timerType == TimerType.CONTRACT_ACCEPTANCE)
            {
                SetEndTime();
            }
        }

        protected void OnLaunch(EventReport er)
        {
            if (timerType == TimerType.NEXT_LAUNCH && endTime == 0.0)
            {
                SetEndTime();
            }
        }

        protected void OnParameterChange(Contract c, ContractParameter p)
        {
            if (c == Root && p.ID == parameter && timerType == TimerType.PARAMETER_COMPLETION && endTime == 0.0)
            {
                SetEndTime();
            }
        }

        private void SetEndTime()
        {
            endTime = Planetarium.GetUniversalTime() + duration;

            // We are completed...  until the time runs out
            SetState(ParameterState.Complete);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (endTime == 0.0)
            {
                return;
            }

            // Every time the clock ticks over, make an attempt to update the contract window
            // title.  We do this because otherwise the window will only ever read the title once,
            // so this is the only way to get our fancy timer to work.
            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                // Boom!
                if (Planetarium.GetUniversalTime() > endTime)
                {
                    SetState(ParameterState.Failed);
                }
                lastUpdate = Planetarium.GetUniversalTime();

                titleTracker.UpdateContractWindow(this, GetTitle());
            }
        }
    }
}
