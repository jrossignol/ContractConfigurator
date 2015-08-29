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
    /// Parameter for ensuring that a certain time must elapse before the contract completes.  Replacement for Duration.
    /// </summary>
    public class Duration2 : VesselParameter
    {
        protected double duration { get; set; }
        protected string preWaitText { get; set; }
        protected string waitingText { get; set; }
        protected string completionText { get; set; }

        private Dictionary<Vessel, double> endTimes = new Dictionary<Vessel, double>();


        private double lastUpdate = 0.0;
        private bool resetClock = false;

        private TitleTracker titleTracker = new TitleTracker();

        public Duration2()
            : this(0.0)
        {
        }

        public Duration2(double duration, string preWaitText = null, string waitingText = null, string completionText = null)
            : base("")
        {
            this.duration = duration;
            this.preWaitText = preWaitText;
            this.waitingText = waitingText;
            this.completionText = completionText;
        }

        protected override string GetParameterTitle()
        {
            Vessel currentVessel = CurrentVessel();

            if (currentVessel != null && endTimes.ContainsKey(currentVessel) && endTimes[currentVessel] > 0.01)
            {
                string title = null;
                if (endTimes[currentVessel] - Planetarium.GetUniversalTime() > 0.0)
                {
                    title = (waitingText ?? "Time to completion:") + " " + DurationUtil.StringValue(endTimes[currentVessel] - Planetarium.GetUniversalTime());
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
                return (preWaitText ?? "Waiting time required:") + " " + DurationUtil.StringValue(duration);
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("duration", duration);
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

            foreach (KeyValuePair<Vessel, double> pair in endTimes)
            {
                ConfigNode childNode = new ConfigNode("VESSEL_END_TIME");
                node.AddNode(childNode);

                childNode.AddValue("vessel", pair.Key.id);
                childNode.AddValue("endTime", pair.Value);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            duration = Convert.ToDouble(node.GetValue("duration"));
            preWaitText = ConfigNodeUtil.ParseValue<string>(node, "preWaitText", (string)null);
            waitingText = ConfigNodeUtil.ParseValue<string>(node, "waitingText", (string)null);
            completionText = ConfigNodeUtil.ParseValue<string>(node, "completionText", (string)null);

            foreach (ConfigNode childNode in node.GetNodes("VESSEL_END_TIME"))
            {
                Vessel v = ConfigNodeUtil.ParseValue<Vessel>(childNode, "vessel");
                if (v != null)
                {
                    double endTime = ConfigNodeUtil.ParseValue<double>(childNode, "endTime");
                    endTimes[v] = endTime;
                }
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnParameterChange(Contract contract, ContractParameter param)
        {
            // Set the end time
            if (contract == Root)
            {
                bool completed = true;
                foreach (ContractParameter child in Parent.GetChildren())
                {
                    if (child == this)
                    {
                        break;
                    }

                    if (child.State != ParameterState.Complete && !child.Optional)
                    {
                        completed = false;
                        break;
                    }
                }

                // Additional check when under a VesselParameterGroup
                VesselParameterGroup vpg = Parent as VesselParameterGroup;
                if (vpg != null && vpg.VesselList.Any())
                {
                    completed &= ContractVesselTracker.Instance.GetAssociatedKeys(FlightGlobals.ActiveVessel).
                        Where(key => vpg.VesselList.Contains(key)).Any();
                }

                Vessel currentVessel = CurrentVessel();
                if (completed && currentVessel != null)
                {
                    if (!endTimes.ContainsKey(currentVessel))
                    {
                        endTimes[currentVessel] = Planetarium.GetUniversalTime() + duration;
                    }
                }
                else
                {
                    if (currentVessel != null && endTimes.ContainsKey(currentVessel))
                    {
                        endTimes.Remove(currentVessel);
                    }
                    resetClock = true;
                }
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                Vessel currentVessel = CurrentVessel();
                double endTime = currentVessel != null && endTimes.ContainsKey(currentVessel) ? endTimes[currentVessel] : 0.0;
                if (endTime != 0.0 || resetClock)
                {
                    lastUpdate = Planetarium.GetUniversalTime();

                    titleTracker.UpdateContractWindow(this, GetTitle());
                    resetClock = false;

                    if (currentVessel != null)
                    {
                        CheckVessel(currentVessel);
                    }
                }
            }
        }

        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            if (vessel == null || !endTimes.ContainsKey(vessel))
            {
                return false;
            }

            return Planetarium.GetUniversalTime() > endTimes[vessel];
        }
    }
}
