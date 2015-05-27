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
    /// ContractParameter that is successful when all child parameters are successful for a the same vessel over the given duration.
    /// </summary>
    public class VesselParameterGroup : ContractConfiguratorParameter
    {
        private const string notePrefix = "<#acfcff>[-] Note: ";
        protected string define { get; set; }
        protected List<string> vesselList { get; set; }
        public IEnumerable<string> VesselList { get { return vesselList; } }
        protected double duration { get; set; }
        protected double completionTime { get; set; }
        protected bool waiting { get; set; }

        private Vessel oldTrackedVessel = null;
        private Vessel trackedVessel = null;
        public Vessel TrackedVessel { get { return trackedVessel; } }
        private Guid trackedVesselGuid = Guid.Empty;

        private double lastUpdate = 0.0;

        private TitleTracker titleTracker = new TitleTracker();

        public VesselParameterGroup()
            : this(null, null, null, 0.0)
        {
        }

        public VesselParameterGroup(string title, string define, IEnumerable<string> vesselList, double duration)
            : base(title)
        {
            this.define = define;
            this.duration = duration;
            this.vesselList = vesselList == null ? new List<string>() : vesselList.ToList();
            waiting = false;
        }

        protected override string GetTitle()
        {
            // Set the first part of the output
            string output;
            if (!string.IsNullOrEmpty(title))
            {
                output = title;
            }
            else
            {
                // Set the vessel name
                output = "Vessel: ";
                if ((waiting || state == ParameterState.Complete) && trackedVessel != null)
                {
                    output += trackedVessel.vesselName;
                }
                else if (!string.IsNullOrEmpty(define))
                {
                    output += define + " (new)";
                }
                else if (vesselList.Any())
                {
                    bool first = true;
                    foreach (string vesselName in vesselList)
                    {
                        if (!first)
                        {
                            output += " OR ";
                        }
                        if (ContractVesselTracker.Instance != null)
                        {
                            output += ContractVesselTracker.Instance.GetDisplayName(vesselName);
                        }
                        else
                        {
                            LoggingUtil.LogWarning(this, "Unable to get vessel display name for '" + vesselName + "' - ContractVesselTracker is null.  This is likely caused by another ScenarioModule crashing, preventing others from loading.");
                            output += vesselName;
                        }
                        first = false;
                    }
                }
                else
                {
                    output += "Any";
                }
            }

            // Not yet complete, add duration
            if (state != ParameterState.Complete)
            {
                // Add duration
                if (waiting && completionTime - Planetarium.GetUniversalTime() > 0.0)
                {
                    output += "; Time Remaining: " + DurationUtil.StringValue(completionTime - Planetarium.GetUniversalTime());
                }
                else if (duration > 0.0)
                {
                    output += "; Duration: " + DurationUtil.StringValue(duration);
                }
            }
            // If we're complete and a custom title hasn't been provided, try to get a better title
            else if (string.IsNullOrEmpty(title))
            {
                if (ParameterCount == 1)
                {
                    output = "";
                    if (trackedVessel != null)
                    {
                        output += "Vessel: " + trackedVessel.vesselName + ": ";
                    }
                    output += GetParameter(0).Title;
                }
            }

            // Add the string that we returned to the titleTracker.  This is used to update
            // the contract title element in the GUI directly, as it does not support dynamic
            // text.
            titleTracker.Add(output);

            return output;
        }

        protected override string GetNotes()
        {
            if (duration > 0.0 && Root.ContractState == Contract.State.Active)
            {
                if (trackedVessel == null)
                {
                    return "No vessel currently matching parameters.";
                }
                else if (!waiting)
                {
                    return "Active Vessel:";
                }
                else
                {
                    return "Waiting for completion time for " + trackedVessel.vesselName + ".";
                }
            }

            return base.GetNotes();
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }

        /// <summary>
        /// Checks the child parameters and updates state.
        /// </summary>
        /// <param name="vessel">The vessel to check the state for</param>
        public void UpdateState(Vessel vessel)
        {
            if (!enabled)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "-> UpdateState(" + (vessel != null ? vessel.id.ToString() : "null") + ")");

            // If this vessel doesn't match our list of valid vessels, ignore the update
            if (!VesselCanBeConsidered(vessel))
            {
                LoggingUtil.LogVerbose(this, "<- UpdateState");
                return;
            }

            // Ignore updates to non-tracked vessels if that vessel is already winning
            if (vessel != trackedVessel && (waiting || state == ParameterState.Complete))
            {
                // Make sure that the state of our tracked vessel has not suddenly changed
                SetChildState(trackedVessel);
                if (AllChildParametersComplete())
                {
                    LoggingUtil.LogVerbose(this, "<- UpdateState");
                    return;
                }
            }

            // Temporarily change the state
            SetChildState(vessel);

            // Check if this is a completion
            if (AllChildParametersComplete())
            {
                trackedVessel = vessel;
                trackedVesselGuid = trackedVessel.id;
            }
            // Look at all other possible craft to see if we can find a winner
            else
            {
                trackedVessel = null;

                // Get a list of vessels to check
                Dictionary<Vessel, int> vessels = new Dictionary<Vessel, int>();
                foreach (VesselParameter p in AllDescendents<VesselParameter>())
                {
                    foreach (Vessel v in p.GetCompletingVessels())
                    {
                        if (v != vessel && VesselCanBeConsidered(v))
                        {
                            vessels[v] = 0;
                        }
                    }
                }

                // Check the vessels
                foreach (Vessel v in vessels.Keys)
                {
                    // Temporarily change the state
                    SetChildState(v);

                    // Do a check
                    if (AllChildParametersComplete())
                    {
                        trackedVessel = v;
                        trackedVesselGuid = trackedVessel.id;
                        break;
                    }
                }

                // Still no winner
                if (trackedVessel == null)
                {
                    // Use active
                    if (FlightGlobals.ActiveVessel != null && VesselCanBeConsidered(FlightGlobals.ActiveVessel))
                    {
                        SetChildState(FlightGlobals.ActiveVessel);
                        trackedVessel = FlightGlobals.ActiveVessel;
                        trackedVesselGuid = trackedVessel.id;
                    }
                }
            }

            // Force a VesselMeetsCondition call to update ParameterDelegate objects
            if (oldTrackedVessel != trackedVessel && trackedVessel != null)
            {
                foreach (ContractParameter p in this.GetAllDescendents())
                {
                    if (p is VesselParameter)
                    {
                        ((VesselParameter)p).CheckVesselMeetsCondition(trackedVessel);
                    }
                }
                oldTrackedVessel = trackedVessel;
            }

            // Fire the parameter change event to account for all the changed child parameters.
            // We don't fire it for the child parameters, as any with a failed state will cause
            // the contract to fail, which we don't want.
            ContractConfigurator.OnParameterChange.Fire(this.Root, this);

            // Manually run the OnParameterStateChange
            OnParameterStateChange(this);

            LoggingUtil.LogVerbose(this, "<- UpdateState");
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("define", define);
            foreach (string vesselName in vesselList)
            {
                node.AddValue("vessel", vesselName);
            }
            node.AddValue("duration", duration);
            if (waiting || state == ParameterState.Complete)
            {
                if (waiting)
                {
                    node.AddValue("completionTime", completionTime);
                }
                node.AddValue("trackedVessel", trackedVesselGuid);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            define = node.GetValue("define");
            duration = Convert.ToDouble(node.GetValue("duration"));
            vesselList = ConfigNodeUtil.ParseValue<List<string>>(node, "vessel", new List<string>());
            if (node.HasValue("completionTime"))
            {
                waiting = true;
                completionTime = Convert.ToDouble(node.GetValue("completionTime"));
            }
            else
            {
                waiting = false;
            }

            if (node.HasValue("trackedVessel"))
            {
                trackedVesselGuid = new Guid(node.GetValue("trackedVessel"));
                trackedVessel = FlightGlobals.Vessels.Find(v => v != null && v.id == trackedVesselGuid);
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onVesselRename.Add(new EventData<GameEvents.HostedFromToAction<Vessel, string>>.OnEvent(OnVesselRename));
            ContractVesselTracker.OnVesselAssociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onVesselRename.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, string>>.OnEvent(OnVesselRename));
            ContractVesselTracker.OnVesselAssociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
        }

        protected void OnVesselAssociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            // If it's the tracked vessel
            if (define == hta.target)
            {
                if (trackedVessel != hta.host)
                {
                    // It's the new tracked vessel
                    trackedVessel = hta.host;
                    trackedVesselGuid = hta.host.id;

                    // Try it out
                    UpdateState(hta.host);
                }
            }
            // If it's a vessel we're looking for
            else if (vesselList.Contains(hta.target))
            {
                // Try it out
                UpdateState(hta.host);

                // Potentially force a title update
                GetTitle();
            }
        }

        protected void OnVesselDisassociation(GameEvents.HostTargetAction<Vessel, string> hta)
        {
            // If it's a vessel we're looking for, and it's tracked
            if (vesselList.Contains(hta.target) && define == hta.target)
            {
                waiting = false;
                trackedVessel = null;
                trackedVesselGuid = Guid.Empty;

                // Try out the active vessel
                UpdateState(FlightGlobals.ActiveVessel);

                // Active vessel didn't work out - force the children to be incomplete
                if (trackedVessel == null)
                {
                    SetChildState(null);

                    // Fire the parameter change event to account for all the changed child parameters.
                    // We don't fire it for the child parameters, as any with a failed state will cause
                    // the contract to fail, which we don't want.
                    ContractConfigurator.OnParameterChange.Fire(this.Root, this);

                    // Manually run the OnParameterStateChange
                    OnParameterStateChange(this);
                }
            }
        }

        protected void OnVesselRename(GameEvents.HostedFromToAction<Vessel, string> hft)
        {
            if (!string.IsNullOrEmpty(define) && trackedVessel == hft.host)
            {
                ContractConfigurator.OnParameterChange.Fire(this.Root, this);
            }
        }

        protected void OnVesselChange(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "OnVesselChange(" + (vessel != null && vessel.id != null ? vessel.id.ToString() : "null") + "), Active = " +
                (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.id != null ? FlightGlobals.ActiveVessel.id.ToString() : "null"));
            UpdateState(vessel);
        }

        protected override void OnParameterStateChange(ContractParameter contractParameter)
        {
            if (System.Object.ReferenceEquals(contractParameter.Parent, this) ||
                System.Object.ReferenceEquals(contractParameter, this))
            {
                if (AllChildParametersComplete())
                {
                    if (!waiting)
                    {
                        waiting = true;
                        completionTime = Planetarium.GetUniversalTime() + duration;

                        // Set the tracked vessel association
                        if (!string.IsNullOrEmpty(define))
                        {
                            ContractVesselTracker.Instance.AssociateVessel(define, trackedVessel);
                        }
                    }
                }
                else
                {
                    waiting = false;
                    if (state == ParameterState.Complete)
                    {
                        SetState(ParameterState.Incomplete);

                        // Set the tracked vessel association
                        if (!string.IsNullOrEmpty(define))
                        {
                            ContractVesselTracker.Instance.AssociateVessel(define, null);
                        }
                    }

                    // Find any failed non-VesselParameter parameters
                    for (int i = 0; i < ParameterCount; i++)
                    {
                        ContractParameter param = GetParameter(i);
                        if (!param.GetType().IsSubclassOf(typeof(VesselParameter)) && param.State == ParameterState.Failed)
                        {
                            SetState(ParameterState.Failed);
                            break;
                        }
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            if (waiting && Planetarium.GetUniversalTime() > completionTime)
            {
                waiting = false;
                SetState(ParameterState.Complete);
            }
            // Every time the clock ticks over, make an attempt to update the contract window
            // notes.  We do this because otherwise the window will only ever read the notes once,
            // so this is the only way to get our fancy timer to work.
            else if (waiting && trackedVessel != null && Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                lastUpdate = Planetarium.GetUniversalTime();

                titleTracker.UpdateContractWindow(this, GetTitle());
            }
        }

        protected IEnumerable<T> AllDescendents<T>() where T : ContractParameter
        {
            return AllDescendents<T>(this);
        }

        protected static IEnumerable<T> AllDescendents<T>(ContractParameter p) where T : ContractParameter
        {
            for (int i = 0; i < p.ParameterCount; i++)
            {
                ContractParameter child = p.GetParameter(i);
                if (child is T)
                {
                    yield return child as T;
                }
                foreach (ContractParameter grandChild in AllDescendents<T>(child))
                {
                    yield return grandChild as T;
                }
            }
        }

        /// <summary>
        /// Set the state in all children to that of the given vessel.
        /// </summary>
        /// <param name="vessel">Vessel to use for the state change</param>
        protected void SetChildState(Vessel vessel)
        {
            foreach (VesselParameter p in AllDescendents<VesselParameter>())
            {
                p.SetState(vessel);
            }
        }

        /// <summary>
        /// Checks whether the given veseel can be considered for completion of this group.  Checks
        /// the vessel inclusion list.
        /// </summary>
        /// <param name="vessel">The vessel to check.</param>
        /// <returns>Whether we can continue with this vessel.</returns>
        private bool VesselCanBeConsidered(Vessel vessel)
        {
            return !vesselList.Any() || vesselList.Any(key => ContractVesselTracker.Instance.GetAssociatedVessel(key) == vessel);
        }
    }
}
