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
        protected string vesselName { get; set; }
        protected double duration { get; set; }
        protected double completionTime { get; set; }
        protected bool waiting { get; set; }

        private Vessel trackedVessel = null;
        private Guid trackedVesselGuid = new Guid();

        private double lastUpdate = 0.0f;

        private Dictionary<string, string> noteTracker = new Dictionary<string, string>();

        public VesselParameterGroup()
            : this(null, null, 0.0)
        {
        }

        public VesselParameterGroup(string title, string vesselName, double duration)
            : base()
        {
            this.vesselName = vesselName;
            this.duration = duration;
            if (title != null)
            {
                this.title = title;

            }
            else
            {
                this.title = "Vessel: Any";
                if (duration > 0.0)
                {
                    this.title += ";\n Duration: " + DurationUtil.StringValue(duration);
                }
            }
            waiting = false;
        }

        protected override string GetTitle()
        {
            return title;
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
        public void UpdateState()
        {
            int max = 0;
            int requiredParameters = 0;
            Dictionary<Vessel, int> counts = new Dictionary<Vessel, int>();

            // Figure out the currently winning vessel
            for (int i = 0; i < ParameterCount; i++)
            {
                ContractParameter p = GetParameter(i);
                if (p.GetType().IsSubclassOf(typeof(VesselParameter)))
                {
                    requiredParameters++;
                    foreach (Vessel v in ((VesselParameter)p).GetCompletingVessels())
                    {
                        if (!counts.ContainsKey(v))
                        {
                            counts[v] = 0;
                        }
                        max = Math.Max(++counts[v], max);
                    }
                }
            }

            // Nobody matches anything yet!
            if (counts.Count == 0)
            {
                return;
            }

            // Someone has completed
            if (max == requiredParameters)
            {
                // Get the winning vessel
                if (trackedVessel == null || !counts.ContainsKey(trackedVessel) || counts[trackedVessel] < max)
                {
                    IEnumerable<KeyValuePair<Vessel, int>> matches = counts.Where(p => p.Value == max);
                    double minVessel = Double.MaxValue;
                    foreach (KeyValuePair<Vessel, int> pair in matches)
                    {
                        double maxParam = 0.0;
                        for (int i = 0; i < ParameterCount; i++)
                        {
                            ContractParameter p = GetParameter(i);
                            if (p.GetType().IsSubclassOf(typeof(VesselParameter)))
                            {
                                maxParam = Math.Max(maxParam, ((VesselParameter)p).GetCompletionTime(pair.Key));
                            }
                        }
                        if (maxParam < minVessel)
                        {
                            minVessel = maxParam;
                            trackedVessel = pair.Key;
                            trackedVesselGuid = trackedVessel.id;
                        }
                    }

                    // Use the time of the selected vessel for the completion time
                    if (AllChildParametersComplete())
                    {
                        waiting = true;
                        completionTime = minVessel + duration;
                    }

                }
            }
            // Use the active vessel
            else if (FlightGlobals.ActiveVessel != null)
            {
                trackedVessel = FlightGlobals.ActiveVessel;
                trackedVesselGuid = trackedVessel.id;
            }

            // Set the state based on the tracked vessel
            for (int i = 0; i < ParameterCount; i++)
            {
                ContractParameter p = GetParameter(i);
                if (p.GetType().IsSubclassOf(typeof(VesselParameter)))
                {
                    ((VesselParameter)p).SetState(trackedVessel);
                }
            }

            // Manually run the OnParameterStateChange
            OnParameterStateChange(this);
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("duration", duration);
            if (waiting)
            {
                node.AddValue("completionTime", completionTime);
                node.AddValue("trackedVessel", trackedVesselGuid);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            duration = Convert.ToDouble(node.GetValue("duration"));
            if (node.HasValue("completionTime"))
            {
                waiting = true;
                completionTime = Convert.ToDouble(node.GetValue("completionTime"));
                trackedVesselGuid = new Guid(node.GetValue("trackedVessel"));
                trackedVessel = FlightGlobals.Vessels.Find(v => v.id == trackedVesselGuid);
            }
            else
            {
                waiting = false;
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
        }

        protected void OnVesselChange(Vessel vessel)
        {
            UpdateState();
        }

        protected override void OnParameterStateChange(ContractParameter contractParameter)
        {
            if (AllChildParametersComplete())
            {
                waiting = true;
                completionTime = Planetarium.GetUniversalTime() + duration;
            }
            else
            {
                waiting = false;

                // Find any failed non-VesselParameter parameters
                for (int i = 0; i < ParameterCount; i++)
                {
                    ContractParameter param = GetParameter(i);
                    if (!param.GetType().IsSubclassOf(typeof(VesselParameter)) && param.State == ParameterState.Failed) {
                        SetFailed();
                        break;
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
    }
}
