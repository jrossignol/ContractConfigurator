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
        protected double duration { get; set; }
        protected double endTime { get; set; }

        private double lastUpdate = 0.0;

        private TitleTracker titleTracker = new TitleTracker();

        public Timer()
            : this(0.0)
        {
        }

        public Timer(double duration)
            : base("")
        {
            this.duration = duration;
            endTime = 0.0;
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
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            duration = Convert.ToDouble(node.GetValue("duration"));
            endTime = Convert.ToDouble(node.GetValue("endTime"));
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected void OnContractAccepted(Contract contract)
        {
            // Set the end time
            if (contract == Root)
            {
                endTime = Planetarium.GetUniversalTime() + duration;
                
                // We are completed...  until the time runs out
                SetState(ParameterState.Complete);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

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
