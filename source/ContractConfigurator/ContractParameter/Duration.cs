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
    /// Parameter for ensuring that a certain time must elapse before the contract completes.
    /// </summary>
    public class Duration : ContractParameter
    {
        protected double duration { get; set; }
        protected double endTime { get; set; }
        protected string preWaitText { get; set; }
        protected string waitingText { get; set; }
        protected string completionText { get; set; }

        private double lastUpdate = 0.0;
        private bool resetClock = false;

        private TitleTracker titleTracker = new TitleTracker();

        public Duration()
            : this(0.0)
        {
        }

        public Duration(double duration, string preWaitText = null, string waitingText = null, string completionText = null)
            : base()
        {
            this.duration = duration;
            this.preWaitText = preWaitText;
            this.waitingText = waitingText;
            this.completionText = completionText;
            endTime = 0.0;
        }

        protected override string GetTitle()
        {
            if (endTime > 0.01)
            {
                string title = null;
                if (endTime - Planetarium.GetUniversalTime() > 0.0)
                {
                    title = (waitingText ?? "Time to completion") + ": " + DurationUtil.StringValue(endTime - Planetarium.GetUniversalTime());
                }
                else
                {
                    title = completionText ?? "Wait time over";
                }

                // Add the string that we returned to the titleTracker.  This is used to update
                // the contract title element in the GUI directly, as it does not support dynamic
                // text.
                titleTracker.Add(title);

                return title;
            }
            else
            {
                return (preWaitText ?? "Waiting time required") + ": " + DurationUtil.StringValue(duration);
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("duration", duration);
            node.AddValue("endTime", endTime);
            if (preWaitText != null)
            {
                node.AddValue("preWaitText", preWaitText);
            }
            if (waitingText != null)
            {
                node.AddValue("waitingText", waitingText);
            }
            if (completionText != null)
            {
                node.AddValue("completionText", completionText);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            duration = Convert.ToDouble(node.GetValue("duration"));
            endTime = Convert.ToDouble(node.GetValue("endTime"));
            preWaitText = ConfigNodeUtil.ParseValue<string>(node, "preWaitText", (string)null);
            waitingText = ConfigNodeUtil.ParseValue<string>(node, "waitingText", (string)null);
            completionText = ConfigNodeUtil.ParseValue<string>(node, "completionText", (string)null);
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnParameterChange(Contract contract, ContractParameter param)
        {
            // Set the end time
            if (contract == Root)
            {
                bool completed = true;
                foreach (ContractParameter child in Root.GetChildren())
                {
                    if (child != this && child.State != ParameterState.Complete)
                    {
                        completed = false;
                        break;
                    }
                }

                if (completed)
                {
                    if (endTime == 0.0)
                    {
                        endTime = Planetarium.GetUniversalTime() + duration;
                    }
                }
                else
                {
                    endTime = 0.0;
                    resetClock = true;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f && (endTime != 0.0 || resetClock))
            {
                // Completed
                if (endTime != 0.0 && Planetarium.GetUniversalTime() > endTime)
                {
                    SetComplete();
                }
                lastUpdate = Planetarium.GetUniversalTime();

                titleTracker.UpdateContractWindow(GetTitle());
                resetClock = false;
            }
        }
    }
}
