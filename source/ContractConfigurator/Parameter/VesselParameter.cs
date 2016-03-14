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
    /// Base class for parameters that support grouping via VesselParameterGroup.
    /// </summary>
    public abstract class VesselParameter : ContractConfiguratorParameter, ParameterDelegateContainer
    {
        /// <summary>
        /// Strength of the parameter - ie. how sure we are that we have completed the parameter.
        /// </summary>
        private enum ParamStrength
        {
            WEAK,   // A craft that once was docked with a craft that met the parameter
            MEDIUM, // A craft that is currently docked to a craft that met the parameter
            STRONG, // A craft that actually met the parameter
        }

        private class VesselInfo
        {
            public Contracts.ParameterState state = ParameterState.Incomplete;
            public double completionTime = 0.0;
            public Vessel vessel = null;
            public Guid id;
            public ParamStrength strength = ParamStrength.STRONG;

            public VesselInfo(Guid id, Vessel vessel)
            {
                this.id = id;
                this.vessel = vessel;
            }
        }
        private Dictionary<Guid, VesselInfo> vesselInfo;
        private Dictionary<uint, KeyValuePair<ParamStrength, double>> dockedVesselInfo;
        private bool allowStateReset = true;

        public bool ChildChanged { get; set; }

        /// <summary>
        /// Set to true in child classes to fail instead of being incomplete when the parameter
        /// conditions are not met.
        /// </summary>
        protected bool failWhenUnmet = false;
        public bool FailWhenUnmet
        {
            get { return failWhenUnmet; }
            set { failWhenUnmet = value; }
        }

        public VesselParameter() : this(null) { }

        public VesselParameter(string title)
            : base(title)
        {
            vesselInfo = new Dictionary<Guid, VesselInfo>();
            dockedVesselInfo = new Dictionary<uint, KeyValuePair<ParamStrength, double>>();
            disableOnStateChange = false;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            // Don't save all this stuff if the parameter is done
            if (!enabled)
            {
                return;
            }

            if (disableOnStateChange)
            {
                disableOnStateChange = false;
                allowStateReset = false;
            }

            // Save state flag
            node.AddValue("allowStateReset", allowStateReset);

            if (failWhenUnmet)
            {
                node.AddValue("failWhenUnmet", failWhenUnmet);
            }

            // Save vessel information
            foreach (KeyValuePair<Guid, VesselInfo> p in vesselInfo.Where(p => p.Value.state != ParameterState.Incomplete))
            {
                ConfigNode child = new ConfigNode("VESSEL_STATS");
                child.AddValue("vessel", p.Key);
                child.AddValue("state", p.Value.state);
                child.AddValue("strength", p.Value.strength);
                if (p.Value.state == ParameterState.Complete)
                {
                    child.AddValue("completionTime", p.Value.completionTime);
                }
                node.AddNode(child);
            }

            // Save docked sub-vessels
            foreach (KeyValuePair<uint, KeyValuePair<ParamStrength, double>> p in dockedVesselInfo)
            {
                ConfigNode child = new ConfigNode("DOCKED_SUB_VESSEL");
                child.AddValue("hash", p.Key);
                child.AddValue("strength", p.Value.Key);
                child.AddValue("completionTime", p.Value.Value);
                node.AddNode(child);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            // Load state flag
            allowStateReset = Convert.ToBoolean(node.GetValue("allowStateReset"));
            failWhenUnmet = ConfigNodeUtil.ParseValue<bool?>(node, "failWhenUnmet", (bool?)false).Value;

            // Load completion times
            foreach (ConfigNode child in node.GetNodes("VESSEL_STATS"))
            {
                Guid id = new Guid(child.GetValue("vessel"));
                Vessel vessel = FlightGlobals.Vessels.Find(v => v != null && v.id == id);

                if (vessel != null || HighLogic.LoadedScene == GameScenes.EDITOR)
                {
                    VesselInfo info = new VesselInfo(id, vessel);
                    info.state = ConfigNodeUtil.ParseValue<ParameterState>(child, "state");
                    info.strength = ConfigNodeUtil.ParseValue<ParamStrength>(child, "strength");
                    info.completionTime = ConfigNodeUtil.ParseValue<double>(child, "completionTime", 0.0);
                    vesselInfo[id] = info;
                }
            }

            // Load docked sub-vessels
            foreach (ConfigNode child in node.GetNodes("DOCKED_SUB_VESSEL"))
            {
                uint hash = Convert.ToUInt32(child.GetValue("hash"));
                ParamStrength strength = ConfigNodeUtil.ParseValue<ParamStrength>(child, "strength");
                double completionTime = ConfigNodeUtil.ParseValue<double>(child, "completionTime", 0.0);
                dockedVesselInfo[hash] = new KeyValuePair<ParamStrength,double>(strength, completionTime);
            }
        }

        /// <summary>
        /// Sets the parameter state for the given vessel.  
        /// </summary>
        /// <param name="vessel">The vessel to set the state for.</param>
        /// <param name="state">State to use.</param>
        /// <returns>Returns true if a state change actually occurred.</returns>
        protected virtual bool SetState(Vessel vessel, Contracts.ParameterState state)
        {
            if (vessel == null)
            {
                return false;
            }

            // Check if the transition is allowed
            if (state == ParameterState.Complete && !ReadyToComplete())
            {
                LoggingUtil.LogVerbose(this, "Not setting state for vessel " + vessel.id + ", not ready to complete!");
                return false;
            }

            LoggingUtil.LogVerbose(this, "SetState to " + state + " for vessel " + vessel.id);

            // Before we wreck anything, don't allow the default disable on state change logic
            if (disableOnStateChange)
            {
                disableOnStateChange = false;
                allowStateReset = false;
            }

            // Initialize
            if (!vesselInfo.ContainsKey(vessel.id))
            {
                vesselInfo[vessel.id] = new VesselInfo(vessel.id, vessel);
            }

            // Set the completion time
            if (state == Contracts.ParameterState.Complete &&
                vesselInfo[vessel.id].state != Contracts.ParameterState.Complete)
            {
                vesselInfo[vessel.id].completionTime = Planetarium.GetUniversalTime();
            }

            // Force to failure if failWhenUnmet is set
            if (failWhenUnmet && state == ParameterState.Incomplete)
            {
                state = ParameterState.Failed;
            }

            // Set the state
            if (allowStateReset || state != ParameterState.Incomplete)
            {
                if (vesselInfo[vessel.id].state != state)
                {
                    vesselInfo[vessel.id].state = state;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the state of the given vessel.
        /// </summary>
        /// <param name="vessel">the vessel to check</param>
        /// <returns>The state for the vessel.</returns>
        protected virtual Contracts.ParameterState GetStateForVessel(Vessel vessel)
        {
            if (!vesselInfo.ContainsKey(vessel.id))
            {
                return ParameterState.Incomplete;
            }

            return vesselInfo[vessel.id].state;
        }

        /// <summary>
        /// Sets the global parameter state to the one of the given vessel
        /// </summary>
        /// <param name="vessel">The vessel to use for the state change.</param>
        public virtual void SetState(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "SetState to that of vessel " + (vessel != null ? vessel.id.ToString() : "null"));

            if (vessel != null && vesselInfo.ContainsKey(vessel.id))
            {
                this.state = vesselInfo[vessel.id].state;
            }
            else
            {
                this.state = ParameterState.Incomplete;
            }
        }

        /// <summary>
        /// Gets the parameter state for the given vessel.
        /// </summary>
        /// <param name="vessel">Vessel to get the state for</param>
        /// <returns>Vessel state</returns>
        public virtual Contracts.ParameterState GetState(Vessel vessel)
        {
            if (vesselInfo.ContainsKey(vessel.id))
            {
                return vesselInfo[vessel.id].state;
            }
            else
            {
                return ParameterState.Incomplete;
            }
        }

        /// <summary>
        /// Gets the completion time for the given vessel.  Returns zero if the vessel isn't
        /// currently completing the conditions.
        /// </summary>
        /// <param name="vessel">Vessel to check completion time for</param>
        /// <returns>The time that the vessel completed the parameter</returns>
        public virtual double GetCompletionTime(Vessel vessel)
        {
            if (vesselInfo.ContainsKey(vessel.id))
            {
                return vesselInfo[vessel.id].completionTime;
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Gets all vessels that are currently completing the parameter.
        /// </summary>
        /// <returns>Iterator of vessels that meet the parameter</returns>
        public virtual IEnumerable<Vessel> GetCompletingVessels()
        {
            return vesselInfo.Where(p => p.Value.state == Contracts.ParameterState.Complete && p.Value.vessel != null).Select(p => p.Value.vessel);
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
            GameEvents.onVesselCreate.Add(new EventData<Vessel>.OnEvent(OnVesselCreate));
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onPartJointBreak.Add(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
            GameEvents.onPartAttach.Add(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(OnPartAttach));
            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
            GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(OnVesselCreate));
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onPartJointBreak.Remove(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
            GameEvents.onPartAttach.Remove(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(OnPartAttach));
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
        }

        protected virtual void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> a)
        {
            // Note that the VesselType of the Kerbal coming out is set to debris initially!  This is
            // probably a bug in stock, and is unreliable in my opinion.  But we can't check that the
            // other is a vessel, as it may be a station or something else.  So we check for both
            // debris or eva, in case this behaviour changes in future.

            // Kerbal going on EVA
            if (a.to.vesselType == VesselType.EVA || a.to.vesselType == VesselType.Debris)
            {
                NewEVA(a.from.vessel, a.to.vessel);
            }

            // Kerbal coming home
            if (a.from.vesselType == VesselType.EVA || a.from.vesselType == VesselType.Debris)
            {
                ReturnEVA(a.to.vessel, a.from.vessel);
            }
        }

        protected void NewEVA(Vessel parent, Vessel eva)
        {
            // Check if there's anything for the parent
            if (vesselInfo.ContainsKey(parent.id))
            {
                // If there's a completion, transfer that to the EVA
                VesselInfo vi = vesselInfo[parent.id];
                if (vi.state == ParameterState.Complete && vi.strength != ParamStrength.WEAK)
                {
                    VesselInfo viEVA = new VesselInfo(eva.id, eva);
                    viEVA.completionTime = vi.completionTime;
                    viEVA.state = vi.state;
                    viEVA.strength = ParamStrength.WEAK;
                    vesselInfo[eva.id] = viEVA;
                }
            }
        }

        protected void ReturnEVA(Vessel parent, Vessel eva)
        {
            // Check if the EVA is interesting...
            if (vesselInfo.ContainsKey(eva.id))
            {
                // Initialize parent
                if (!vesselInfo.ContainsKey(parent.id))
                {
                    vesselInfo[parent.id] = new VesselInfo(parent.id, parent);
                }

                // Copy to parent vessel
                VesselInfo vi = vesselInfo[parent.id];
                VesselInfo viEVA = vesselInfo[eva.id];
                if (viEVA.state == ParameterState.Complete && vi.state != ParameterState.Complete)
                {
                    vi.state = viEVA.state;
                    vi.strength = viEVA.strength;
                    vi.completionTime = viEVA.completionTime;
                }
            }
        }

        protected virtual void OnFlightReady()
        {
            CheckVessel(FlightGlobals.ActiveVessel);

            // Set parameters properly on first load
            if (FlightGlobals.ActiveVessel != null)
            {
                VesselParameterGroup vpg = GetParameterGroupHost();
                if (vpg != null)
                {
                    vpg.UpdateState(FlightGlobals.ActiveVessel);
                }
            }
        }

        protected virtual void OnVesselCreate(Vessel vessel)
        {
            if (IsIgnoredVesselType(vessel.vesselType) || HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "OnVesselCreate(" + vessel.id + ")");

            // Go through the hashes to try to set the parameters for this vessel
            KeyValuePair<ParamStrength, double>? dockedInfo = null;
            foreach (uint hash in vessel.GetHashes())
            {
                if (dockedVesselInfo.ContainsKey(hash))
                {
                    if (dockedInfo == null)
                    {
                        dockedInfo = dockedVesselInfo[hash];
                    }
                    else
                    {
                        dockedInfo = dockedVesselInfo[hash].Key > dockedInfo.Value.Key ? dockedVesselInfo[hash] : dockedInfo;
                    }
                }
            }

            // Found one
            if (dockedInfo != null)
            {
                VesselInfo v = new VesselInfo(vessel.id, vessel);
                v.strength = dockedInfo.Value.Key;
                v.completionTime = dockedInfo.Value.Value;
                v.state = ParameterState.Complete;
                vesselInfo[vessel.id] = v;
                LoggingUtil.LogVerbose(this, "   set state to " + v.state + " and strength to " + v.strength);
            }
            else
            {
                LoggingUtil.LogVerbose(this, "   didn't find docked sub-vessel info");
            }

            CheckVessel(vessel);
        }

        protected virtual void OnVesselChange(Vessel vessel)
        {
            if (vessel != null)
            {
                LoggingUtil.LogVerbose(this, "OnVesselChange(" + vessel.id + ")");
            }

            CheckVessel(vessel);
        }

        protected virtual void OnPartJointBreak(PartJoint p)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR || p.Parent.vessel == null)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "OnPartJointBreak(" + p.Parent.vessel.id + ")");

            // Check if we need to make modifications based on undocking
            if (vesselInfo.ContainsKey(p.Parent.vessel.id) && vesselInfo[p.Parent.vessel.id].state == ParameterState.Complete)
            {
                ParamStrength strength = vesselInfo[p.Parent.vessel.id].strength;

                // Medium strengths indicates that we may need to lower to weak if the "strong"
                // part is lost
                if (strength == ParamStrength.MEDIUM)
                {
                    foreach (uint hash in p.Parent.vessel.GetHashes())
                    {
                        strength = dockedVesselInfo[hash].Key > strength ? dockedVesselInfo[hash].Key : strength;
                    }

                    vesselInfo[p.Parent.vessel.id].strength = strength == ParamStrength.STRONG ? ParamStrength.MEDIUM : ParamStrength.WEAK;
                }
                else if (strength == ParamStrength.STRONG)
                {
                    // Save the sub vessel info
                    SaveSubVesselInfo(p.Parent.vessel, ParamStrength.STRONG, vesselInfo[p.Parent.vessel.id].completionTime);
                }
            }

            CheckVessel(p.Parent.vessel);
        }

        protected virtual void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR || e.host.vessel == null || e.target.vessel == null)
            {
                return;
            }

            // Get the vesselInfo structs
            VesselInfo v1 = vesselInfo.ContainsKey(e.host.vessel.id) ? vesselInfo[e.host.vessel.id] : null;
            VesselInfo v2 = vesselInfo.ContainsKey(e.target.vessel.id) ? vesselInfo[e.target.vessel.id] : null;

            // Handle cases of untracked vessels
            if (v1 == null && v2 == null)
            {
                return;
            }
            else if (v1 == null)
            {
                VesselInfo v = new VesselInfo(e.host.vessel.id, e.host.vessel);
                v1 = vesselInfo[e.host.vessel.id] = v;
            }
            else if (v2 == null)
            {
                VesselInfo v = new VesselInfo(e.target.vessel.id, e.target.vessel);
                v2 = vesselInfo[e.target.vessel.id] = v;
            }

            // Neither is complete, nothing to do
            if (v1.state != ParameterState.Complete && v2.state != ParameterState.Complete)
            {
                return;
            }
            // Both are complete
            else if (v1.state == v2.state)
            {
                // Save the subvessel info
                SaveSubVesselInfo(v1.vessel, v1.strength == ParamStrength.STRONG ? ParamStrength.STRONG : ParamStrength.WEAK, v1.completionTime);
                SaveSubVesselInfo(v2.vessel, v2.strength == ParamStrength.STRONG ? ParamStrength.STRONG : ParamStrength.WEAK, v2.completionTime);

                // Minimize completion time
                v1.completionTime = v2.completionTime = Math.Min(v1.completionTime, v2.completionTime);

                // If the two strengths are different, they both end up medium
                if (v1.strength != v2.strength)
                {
                    v1.strength = v2.strength = ParamStrength.MEDIUM;
                }
            }
            // Only one is complete
            else
            {
                // Swap to make v1 the complete one
                if (v2.state == ParameterState.Complete)
                {
                    VesselInfo tmp = v2;
                    v2 = v1;
                    v1 = tmp;
                }

                // Save the subvessel info for v1 only
                SaveSubVesselInfo(v1.vessel, v1.strength == ParamStrength.STRONG ? ParamStrength.STRONG : ParamStrength.WEAK, v1.completionTime);

                // v1 is complete - transfer parameter state only if strength is not weak
                if (v1.strength != ParamStrength.WEAK)
                {
                    // Save sub-vessel info for v2
                    SaveSubVesselInfo(v2.vessel, ParamStrength.WEAK, v1.completionTime);

                    v2.completionTime = v1.completionTime;
                    v2.state = v1.state;
                    v1.strength = v2.strength = ParamStrength.MEDIUM;
                }
            }

            CheckVessel(e.host.vessel);
            if (e.host.vessel.id != e.target.vessel.id)
            {
                CheckVessel(e.target.vessel);
            }
        }

        private void OnContractAccepted(Contract c)
        {
            if (c != Root || HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                return;
            }

            CheckVessel(FlightGlobals.ActiveVessel);
        }

        /// <summary>
        /// Saves all the sub-vessel information - breaking up the vessels into the smallest
        /// pieces possible.
        /// </summary>
        /// <param name="vessel">The vessel to break up</param>
        /// <param name="strength">The strength of the parameter</param>
        /// <param name="completionTime">The completion time</param>
        private void SaveSubVesselInfo(Vessel vessel, ParamStrength strength, double completionTime)
        {
            foreach (uint hash in vessel.GetHashes())
            {
                if (!dockedVesselInfo.ContainsKey(hash) ||
                    dockedVesselInfo[hash].Key < strength)
                {
                    dockedVesselInfo[hash] = new KeyValuePair<ParamStrength, double>(strength, completionTime);
                }
            }
        }

        /// <summary>
        /// Checks whether the given vessel meets the condition, and completes the contract parameter as necessary.
        /// </summary>
        /// <param name="vessel">The vessel to check.</param>
        protected virtual void CheckVessel(Vessel vessel, bool forceStateChange = false)
        {
            // No vessel to check.
            if (vessel == null)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "-> CheckVessel(" + vessel.id + ")");
            if (IsIgnoredVesselType(vessel.vesselType))
            {
                LoggingUtil.LogVerbose(this, "<- CheckVessel - ignored vessel type (" + vessel.vesselType + ")");
                return;
            }

            VesselParameterGroup vpg = GetParameterGroupHost();

            if (CanCheckVesselMeetsCondition(vessel))
            {
                // Using VesselParameterGroup logic
                if (vpg != null)
                {
                    // Set the craft specific state
                    bool stateChanged = SetState(vessel, VesselMeetsCondition(vessel) ?
                        Contracts.ParameterState.Complete : Contracts.ParameterState.Incomplete) || forceStateChange;

                    // Update the group
                    if (stateChanged)
                    {
                        vpg.UpdateState(vessel);
                    }
                }
                // Logic applies only to active vessel
                else if (vessel.isActiveVessel  || FlightGlobals.ActiveVessel == null)
                {
                    if (VesselMeetsCondition(vessel))
                    {
                        SetState(ParameterState.Complete);
                        if (!allowStateReset)
                        {
                            Disable();
                        }
                    }
                    else if (failWhenUnmet)
                    {
                        SetState(ParameterState.Failed);
                    }
                    else
                    {
                        SetState(ParameterState.Incomplete);
                    }
                }

                // Special handling for parameter delegates
                if (ChildChanged)
                {
                    LoggingUtil.LogVerbose(this, "Firing onParameterChange due to ChildChanged = true");
                    ContractConfigurator.OnParameterChange.Fire(this.Root, this);
                    ChildChanged = false;
                }
            }
            LoggingUtil.LogVerbose(this, "<- CheckVessel");
        }

        protected IEnumerable<Vessel> GetVessels()
        {
            foreach (VesselInfo vi in vesselInfo.Values)
            {
                yield return vi.vessel;
            }
        }

        /// <summary>
        /// Checks if this is one of the ignored vessel types.
        /// </summary>
        /// <param name="vesselType">The type of vessel</param>
        /// <returns>True if this type of vessel should be ignored.</returns>
        public virtual bool IsIgnoredVesselType(VesselType vesselType) 
        {
            switch (vesselType)
            {
                case VesselType.Debris:
                case VesselType.Flag:
                case VesselType.SpaceObject:
                case VesselType.Unknown:
                    return true;
                default:
                    return false;
            }
        }

        protected Vessel CurrentVessel()
        {
            VesselParameterGroup vpg = GetParameterGroupHost();
            return vpg == null ? null : vpg.TrackedVessel;
        }

        protected VesselParameterGroup GetParameterGroupHost()
        {
            IContractParameterHost host = Parent;
            while (host != Root && !(host is VesselParameterGroup))
            {
                host = host.Parent;
            }
            if (host is VesselParameterGroup)
            {
                return (VesselParameterGroup)host;
            }
            return null;
        }

        /// <summary>
        /// Function that determines if we are able to call VesselMeetsCondition for the given
        /// vessel at this time.
        /// </summary>
        /// <param name="vessel">The vessel to check for.</param>
        /// <returns>Whether we are allowed to call VesselMeetsCondition.  If false is returned,
        /// VesselMeetsCondition will not be called, and the vessel's state remains unchanged.</returns>
        protected virtual bool CanCheckVesselMeetsCondition(Vessel vessel)
        {
            if (completeInSequence || Parent is Sequence)
            {
                // Go through the parent's parameters
                for (int i = 0; i < Parent.ParameterCount; i++)
                {
                    ContractParameter param = Parent.GetParameter(i);
                    // If we've made it all the way to us, we're ready
                    if (System.Object.ReferenceEquals(param, this))
                    {
                        // Passed our check
                        break;
                    }
                    else if (param.State != ParameterState.Complete)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void CheckVesselMeetsCondition(Vessel vessel)
        {
            if (CanCheckVesselMeetsCondition(vessel))
            {
                VesselMeetsCondition(vessel);
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected abstract bool VesselMeetsCondition(Vessel vessel);
    }
}
