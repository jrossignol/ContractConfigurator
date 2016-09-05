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
    public class Duration : VesselParameter
    {
        public enum StartCriteria
        {
            CONTRACT_ACCEPTANCE,
            NEXT_LAUNCH,
            PARAMETER_COMPLETION,
            PARAMETER_FAILURE
        }

        protected double duration { get; set; }
        protected string preWaitText { get; set; }
        protected string waitingText { get; set; }
        protected string completionText { get; set; }
        protected StartCriteria startCriteria;
        protected List<string> parameter;
        protected bool triggered = false;

        private Dictionary<Guid, double> endTimes = new Dictionary<Guid, double>();
        private double endTime = 0.0;

        private double lastUpdate = 0.0;
        private bool resetClock = false;
        private double waitTime = double.MaxValue;

        public Duration()
            : base()
        {
            // Queue up a check on startup - give a comfortable delay so we don't have a timer reset
            waitTime = Time.fixedTime + 0.5;
        }

        public Duration(double duration, string preWaitText, string waitingText, string completionText, StartCriteria startCriteria, List<string> parameter)
            : base("")
        {
            this.duration = duration;
            this.preWaitText = preWaitText;
            this.waitingText = waitingText;
            this.completionText = completionText;
            this.startCriteria = startCriteria;
            this.parameter = parameter;
            disableOnStateChange = true;
        }

        protected override string GetParameterTitle()
        {
            Vessel currentVessel = CurrentVessel();

            string title = null;
            if (currentVessel != null && endTimes.ContainsKey(currentVessel.id) && endTimes[currentVessel.id] > 0.01 ||
                currentVessel == null && endTime > 0.01)
            {
                double time = currentVessel != null ? endTimes[currentVessel.id] : endTime;
                if (time - Planetarium.GetUniversalTime() > 0.0)
                {
                    title = (waitingText ?? "Time to completion:") + " " + DurationUtil.StringValue(time - Planetarium.GetUniversalTime());
                }
                else
                {
                    title = completionText ?? "Wait time over";
                }
            }
            else
            {
                title = (preWaitText ?? "Waiting time required:") + " " + DurationUtil.StringValue(duration);
            }

            return title;
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

            foreach (KeyValuePair<Guid, double> pair in endTimes)
            {
                ConfigNode childNode = new ConfigNode("VESSEL_END_TIME");
                node.AddNode(childNode);

                childNode.AddValue("vessel", pair.Key);
                childNode.AddValue("endTime", pair.Value);
            }
            node.AddValue("endTime", endTime);
            node.AddValue("startCriteria", startCriteria);
            foreach (string p in parameter)
            {
                node.AddValue("parameter", p);
            }
            node.AddValue("triggered", triggered);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            duration = Convert.ToDouble(node.GetValue("duration"));
            preWaitText = ConfigNodeUtil.ParseValue<string>(node, "preWaitText", (string)null);
            waitingText = ConfigNodeUtil.ParseValue<string>(node, "waitingText", (string)null);
            completionText = ConfigNodeUtil.ParseValue<string>(node, "completionText", (string)null);
            endTime = ConfigNodeUtil.ParseValue<double>(node, "endTime", 0.0);

            startCriteria = ConfigNodeUtil.ParseValue<StartCriteria>(node, "startCriteria", StartCriteria.CONTRACT_ACCEPTANCE);
            parameter = ConfigNodeUtil.ParseValue<List<string>>(node, "parameter", new List<string>());
            triggered = ConfigNodeUtil.ParseValue<bool?>(node, "triggered", (bool?)true).Value;

            foreach (ConfigNode childNode in node.GetNodes("VESSEL_END_TIME"))
            {
                Guid vesselId = ConfigNodeUtil.ParseValue<Guid>(childNode, "vessel");
                if (vesselId != null)
                {
                    double time = ConfigNodeUtil.ParseValue<double>(childNode, "endTime");
                    endTimes[vesselId] = time;
                }
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
            GameEvents.onLaunch.Add(new EventData<EventReport>.OnEvent(OnLaunch));
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
            GameEvents.onLaunch.Remove(new EventData<EventReport>.OnEvent(OnLaunch));
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnContractAccepted(Contract contract)
        {
            // Set the end time
            if (contract == Root && startCriteria == StartCriteria.CONTRACT_ACCEPTANCE)
            {
                triggered = true;
            }
        }

        protected void OnLaunch(EventReport er)
        {
            if (startCriteria == StartCriteria.NEXT_LAUNCH)
            {
                triggered = true;
            }
        }

        protected void OnParameterChange(Contract contract, ContractParameter param)
        {
            if (contract != Root)
            {
                return;
            }

            if (!triggered && parameter.Contains(param.ID))
            {
                if (startCriteria == StartCriteria.PARAMETER_COMPLETION && param.State == ParameterState.Complete ||
                    startCriteria == StartCriteria.PARAMETER_FAILURE && param.State == ParameterState.Failed)
                {
                    triggered = true;
                }
            }

            // Queue up a check
            if (contract == Root)
            {
                waitTime = Time.fixedTime + 0.5;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Do a check to see if we need to play with the end time
            if (waitTime < Time.fixedTime)
            {
                waitTime = double.MaxValue;

                bool completed = triggered;
                if (startCriteria != StartCriteria.PARAMETER_COMPLETION && startCriteria != StartCriteria.PARAMETER_FAILURE)
                {
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
                }

                // Additional check when under a VesselParameterGroup
                VesselParameterGroup vpg = Parent as VesselParameterGroup;
                if (vpg != null && vpg.VesselList.Any())
                {
                    completed &= ContractVesselTracker.Instance.GetAssociatedKeys(FlightGlobals.ActiveVessel).
                        Where(key => vpg.VesselList.Contains(key)).Any();
                }

                Vessel currentVessel = CurrentVessel();
                if (completed)
                {
                    if (currentVessel != null)
                    {
                        if (!endTimes.ContainsKey(currentVessel.id))
                        {
                            endTimes[currentVessel.id] = Planetarium.GetUniversalTime() + duration;
                        }
                    }
                    // Handle case for not under a VesselParameterGroup
                    else if (vpg == null)
                    {
                        if (endTime == 0.0)
                        {
                            endTime = Planetarium.GetUniversalTime() + duration;
                        }
                    }
                }
                else
                {
                    if (currentVessel != null && endTimes.ContainsKey(currentVessel.id))
                    {
                        endTimes.Remove(currentVessel.id);
                    }
                    else if (vpg == null)
                    {
                        endTime = 0.0;
                    }
                    resetClock = true;
                }
            }

            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                Vessel currentVessel = CurrentVessel();
                double time = currentVessel != null && endTimes.ContainsKey(currentVessel.id) ? endTimes[currentVessel.id] : endTime;
                if (time != 0.0 || resetClock)
                {
                    lastUpdate = Planetarium.GetUniversalTime();
                    resetClock = false;

                    // Force a call to GetTitle to update the contracts app
                    GetTitle();

                    if (currentVessel != null)
                    {
                        CheckVessel(currentVessel);
                    }

                    // Special case for non-vessel parameter
                    if (endTime != 0.0 && Planetarium.GetUniversalTime() > endTime)
                    {
                        SetState(ParameterState.Complete);
                    }
                }
            }
        }

        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            if (vessel == null || !endTimes.ContainsKey(vessel.id))
            {
                return false;
            }

            return Planetarium.GetUniversalTime() > endTimes[vessel.id];
        }
    }
}
