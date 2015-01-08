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
    /*
     * ContractParameter that is successful when all child parameters are successful for a the same vessel over the given duration.
     */
    public class VesselParameterGroup : Contracts.ContractParameter
    {
        private const string notePrefix = "<#acfcff>[-] Note: ";
        protected string title { get; set; }
        protected string define { get; set; }
        protected double duration { get; set; }
        protected double completionTime { get; set; }
        protected bool waiting { get; set; }

        private Vessel oldTrackedVessel = null;
        private Vessel trackedVessel = null;
        public Vessel TrackedVessel { get { return trackedVessel; } }
        private Guid trackedVesselGuid = new Guid();

        private double lastUpdate = 0.0f;

        private Dictionary<string, string> noteTracker = new Dictionary<string, string>();

        public VesselParameterGroup()
            : this(null, null, 0.0)
        {
        }

        public VesselParameterGroup(string title, string define, double duration)
            : base()
        {
            this.define = define;
            this.duration = duration;
            this.title = title;
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
                string vesselStr = (waiting || state == ParameterState.Complete) && trackedVessel != null ? trackedVessel.vesselName
                    : string.IsNullOrEmpty(define) ? "Any" : (define + " (new)");
                output = "Vessel: " + vesselStr;
            }

            // Not yet complete, add duration
            if (state != ParameterState.Complete)
            {
                // Add duration
                if (duration > 0.0)
                {
                    output += ";\n Duration: " + DurationUtil.StringValue(duration);
                }
            }
            // If we're complete and a custom title hasn't been provided, try to get a better title
            else if (!string.IsNullOrEmpty(title))
            {
                if (ParameterCount == 1)
                {
                    return GetParameter(0).Title;
                }
            }

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
                    string note = "Time remaining for " + trackedVessel.vesselName + ": " +
                        DurationUtil.StringValue(completionTime - Planetarium.GetUniversalTime());

                    // Add the string that we returned to the noteTracker.  This is used to update
                    // the contract notes element in the GUI directly, as it does support dynamic
                    // text.
                    noteTracker[notePrefix + note] = note;

                    return note;
                }
            }

            return base.GetNotes();
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }

        /*
         * Checks the child parameters and updates state.
         */
        public void UpdateState(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "-> UpdateState(" + (vessel != null ? vessel.id.ToString() : "null") + ")");

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
                        if (v != vessel)
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
                    if (FlightGlobals.ActiveVessel != null)
                    {
                        SetChildState(FlightGlobals.ActiveVessel);
                        trackedVessel = FlightGlobals.ActiveVessel;
                        trackedVesselGuid = trackedVessel.id;
                    }
                    // No active?  Use what was given to us
                    else
                    {
                        SetChildState(vessel);
                        trackedVessel = vessel;
                        trackedVesselGuid = vessel.id;
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
            GameEvents.Contract.onParameterChange.Fire(this.Root, this);

            // Manually run the OnParameterStateChange
            OnParameterStateChange(this);

            LoggingUtil.LogVerbose(this, "<- UpdateState");
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("define", define);
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

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            define = node.GetValue("define");
            duration = Convert.ToDouble(node.GetValue("duration"));
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
                trackedVessel = FlightGlobals.Vessels.Find(v => v.id == trackedVesselGuid);
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onVesselRename.Add(new EventData<GameEvents.HostedFromToAction<Vessel, string>>.OnEvent(OnVesselRename));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onVesselRename.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, string>>.OnEvent(OnVesselRename));
        }

        protected void OnVesselRename(GameEvents.HostedFromToAction<Vessel, string> hft)
        {
            if (!string.IsNullOrEmpty(define) && trackedVessel == hft.host)
            {
                GameEvents.Contract.onParameterChange.Fire(Root, this);
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

                        // Set the current craft as the one matching the name
                        if (!string.IsNullOrEmpty(define) && trackedVessel != null)
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
                        SetIncomplete();
                    }

                    // Make sure no craft is matching the name
                    if (!string.IsNullOrEmpty(define))
                    {
                        ContractVesselTracker.Instance.AssociateVessel(define, null);
                    }

                    // Find any failed non-VesselParameter parameters
                    for (int i = 0; i < ParameterCount; i++)
                    {
                        ContractParameter param = GetParameter(i);
                        if (!param.GetType().IsSubclassOf(typeof(VesselParameter)) && param.State == ParameterState.Failed)
                        {
                            SetFailed();
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
                SetComplete();
            }
            // Every time the clock ticks over, make an attempt to update the contract window
            // notes.  We do this because otherwise the window will only ever read the notes once,
            // so this is the only way to get our fancy timer to work.
            else if (waiting && trackedVessel != null && Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                lastUpdate = Planetarium.GetUniversalTime();

                // Go through all the list items in the contracts window
                UIScrollList list = ContractsApp.Instance.cascadingList.cascadingList;
                for (int i = 0; i < list.Count; i++)
                {
                    // Try to find a rich text control that matches the expected text
                    UIListItemContainer listObject = (UIListItemContainer)list.GetItem(i);
                    SpriteTextRich richText = listObject.GetComponentInChildren<SpriteTextRich>();
                    if (richText != null && noteTracker.ContainsKey(richText.Text))
                    {
                        // Clear the noteTracker, and replace the text
                        noteTracker.Clear();
                        richText.Text = notePrefix + GetNotes();
                    }
                }
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

        /*
         * Set the state in all children to that of the given vessel.
         */
        protected void SetChildState(Vessel vessel)
        {
            foreach (VesselParameter p in AllDescendents<VesselParameter>())
            {
                p.SetState(vessel);
            }
        }
    }
}
