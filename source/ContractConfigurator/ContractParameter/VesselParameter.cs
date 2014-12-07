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
     * Base class for parameters that support grouping via VesselParameterGroup.
     */
    public abstract class VesselParameter : Contracts.ContractParameter
    {
        /*
         * Strength of the parameter - ie. how sure we are that we have completed the parameter.
         */
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
        private Dictionary<uint, ParamStrength> dockedVesselStrength;
        private bool allowStateReset = true;

        /*
         * Set to true in child classes to fail instead of being incomplete when the parameter
         * conditions are not met.
         */
        protected bool failWhenUnmet = false;

        public VesselParameter()
            : base()
        {
            vesselInfo = new Dictionary<Guid, VesselInfo>();
            dockedVesselStrength = new Dictionary<uint, ParamStrength>();
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Don't save all this stuff if the parameter is done
            if (!enabled)
            {
                return;
            }

            // Save state flag
            node.AddValue("allowStateReset", allowStateReset);

            // Save vessel information
            foreach (KeyValuePair<Guid, VesselInfo> p in vesselInfo)
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
            foreach (KeyValuePair<uint, ParamStrength> p in dockedVesselStrength)
            {
                ConfigNode child = new ConfigNode("DOCKED_SUB_VESSEL");
                child.AddValue("hash", p.Key);
                child.AddValue("strength", p.Value);
                node.AddNode(child);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Load state flag
            allowStateReset = Convert.ToBoolean(node.GetValue("allowStateReset"));

            // Load completion times
            foreach (ConfigNode child in node.GetNodes("VESSEL_STATS"))
            {
                Guid id = new Guid(child.GetValue("vessel"));
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == id);

                if (vessel != null || HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH)
                {
                    VesselInfo info = new VesselInfo(id, vessel);
                    info.state = (ParameterState)Enum.Parse(typeof(ParameterState), child.GetValue("state"));
                    info.strength = (ParamStrength)Enum.Parse(typeof(ParamStrength), child.GetValue("strength"));
                    if (state == ParameterState.Complete)
                    {
                        info.completionTime = Convert.ToDouble(child.GetValue("completionTime"));
                    }
                    vesselInfo[id] = info;
                }
            }

            // Load docked sub-vessels
            foreach (ConfigNode child in node.GetNodes("DOCKED_SUB_VESSEL"))
            {
                uint hash = Convert.ToUInt32(child.GetValue("hash"));
                ParamStrength strength = (ParamStrength)Enum.Parse(typeof(ParamStrength), child.GetValue("strength"));
                dockedVesselStrength[hash] = strength;
            }
        }

        /*
         * Sets the parameter state for the given vessel.
         */
        protected virtual void SetState(Vessel vessel, Contracts.ParameterState state)
        {
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
                vesselInfo[vessel.id].state = state;
            }
        }

        /*
         * Sets the global parameter state to the one of the given vessel
         */
        public virtual void SetState(Vessel vessel)
        {
            if (vesselInfo.ContainsKey(vessel.id)) 
            {
                this.state = vesselInfo[vessel.id].state;
            }
            else
            {
                this.state = ParameterState.Incomplete;
            }

            // Fire the parameter change event for the *parent* - otherwise the failed state will
            // cause the contract to fail, which we don't want.
            GameEvents.Contract.onParameterChange.Fire(this.Root, (ContractParameter)this.Parent);
        }

        /*
         * Gets the parameter state for the given vessel.
         */
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

        /*
         * Gets the completion time for the given vessel.  Returns zero if the vessel isn't
         * currently completing the conditions.
         */
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

        /*
         * Gets all vessels that are currently completing the parameter.
         */
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
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
            GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(OnVesselCreate));
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onPartJointBreak.Remove(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
            GameEvents.onPartAttach.Remove(new EventData<GameEvents.HostTargetAction<Part, Part>>.OnEvent(OnPartAttach));
        }

        protected virtual void OnFlightReady()
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected virtual void OnVesselCreate(Vessel vessel)
        {
            Debug.Log("VesselParameter: OnVesselCreate: " + vessel.id);

            if (IsIgnoredVesselType(vessel.vesselType))
            {
                return;
            }

            // Go through the hashes to try to set the parameters for this vessel
            ParamStrength? strength = null;
            foreach (uint hash in GetVesselHashes(vessel))
            {
                if (dockedVesselStrength.ContainsKey(hash))
                {
                    if (strength == null)
                    {
                        strength = dockedVesselStrength[hash];
                    }
                    else
                    {
                        strength = dockedVesselStrength[hash] > strength ? dockedVesselStrength[hash] : strength;
                    }
                }
            }

            // Found one
            if (strength != null)
            {
                VesselInfo v = new VesselInfo(vessel.id, vessel);
                v.strength = (ParamStrength)strength;
                v.state = ParameterState.Complete;
                vesselInfo[vessel.id] = v;
            }

            CheckVessel(vessel);
        }

        protected virtual void OnVesselChange(Vessel vessel)
        {
            Debug.Log("VesselParameter: OnVesselChange: " + vessel.id);

            CheckVessel(vessel);
        }

        protected virtual void OnPartJointBreak(PartJoint p)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH)
            {
                return;
            }
            Debug.Log("VesselParameter: OnPartJointBreak: " + p.Child.vessel.id);

            // Check if we need to make modifications based on undocking
            if (vesselInfo.ContainsKey(p.Parent.vessel.id) && vesselInfo[p.Parent.vessel.id].state == ParameterState.Complete)
            {
                ParamStrength? strength = vesselInfo[p.Parent.vessel.id].strength;

                // Medium strengths indicates that we may need to lower to weak if the "strong"
                // part is lost
                if (strength == ParamStrength.MEDIUM)
                {
                    strength = null;
                    foreach (uint hash in GetVesselHashes(p.Parent.vessel))
                    {
                        if (dockedVesselStrength.ContainsKey(hash))
                        {
                            if (strength == null || strength == dockedVesselStrength[hash])
                            {
                                dockedVesselStrength[hash] = dockedVesselStrength[hash];
                            }
                            else
                            {
                                strength = ParamStrength.MEDIUM;
                            }
                        }
                    }

                    // Shouldn't still be null...
                    if (strength == null)
                    {
                        Debug.LogWarning("ContractConfigurator: Unexpected value when undocking!  Raise a GitHub issue!");
                        strength = ParamStrength.WEAK;
                    }

                    vesselInfo[p.Parent.vessel.id].strength = (ParamStrength)strength;
                }
                else if (strength == ParamStrength.STRONG)
                {
                    // Save the sub vessel info
                    SaveSubVesselInfo(p.Parent.vessel, ParamStrength.STRONG);
                }
            }
        }

        protected virtual void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH)
            {
                return;
            }

            Debug.Log("VesselParameter: OnPartAttach: " + e.host.vessel.id);

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
                SaveSubVesselInfo(v1.vessel, v1.strength == ParamStrength.STRONG ? ParamStrength.STRONG : ParamStrength.WEAK);
                SaveSubVesselInfo(v2.vessel, v2.strength == ParamStrength.STRONG ? ParamStrength.STRONG : ParamStrength.WEAK);

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
                SaveSubVesselInfo(v1.vessel, v1.strength == ParamStrength.STRONG ? ParamStrength.STRONG : ParamStrength.WEAK);

                // v1 is complete - transfer parameter state only if strength is not weak
                if (v1.strength != ParamStrength.WEAK)
                {
                    // Save sub-vessel info for v2
                    SaveSubVesselInfo(v2.vessel, ParamStrength.WEAK);

                    v2.completionTime = v1.completionTime;
                    v2.state = v1.state;
                    v1.strength = v2.strength = ParamStrength.MEDIUM;
                }
            }
        }

        /*
         * Saves all the sub-vessel information - breaking up the vessels into the smallest
         * pieces possible.
         */
        private void SaveSubVesselInfo(Vessel vessel, ParamStrength strength)
        {
            foreach (uint hash in GetVesselHashes(vessel))
            {
                if (!dockedVesselStrength.ContainsKey(hash) ||
                    dockedVesselStrength[hash] < strength)
                {
                    dockedVesselStrength[hash] = strength;
                }
            }
        }

        /*
         * Create a hash of the vessel.
         */
        public static List<uint> GetVesselHashes(Vessel vessel)
        {
            Queue<Part> queue = new Queue<Part>();
            Dictionary<Part, int> visited = new Dictionary<Part, int>();
            Dictionary<uint, uint> dockedParts = new Dictionary<uint, uint>();
            Queue<Part> otherVessel = new Queue<Part>();
            
            // Add the root
            queue.Enqueue(vessel.rootPart);
            visited[vessel.rootPart] = 1;

            // Do a BFS of all parts.
            List<uint> hashes = new List<uint>();
            uint hash = 0;
            while (queue.Count > 0 || otherVessel.Count > 0)
            {
                bool decoupler = false;

                // Start a new ship
                if (queue.Count == 0)
                {
                    // Reset our hash
                    hashes.Add(hash);
                    hash = 0;

                    // Find an unhandled part to use as the new vessel
                    Part px;
                    while (px = otherVessel.Dequeue()) {
                        if (visited[px] != 2)
                        {
                            queue.Enqueue(px);
                            break;
                        }
                    }
                    dockedParts.Clear();
                    continue;
                }

                Part p = queue.Dequeue();

                // Check if this is for a new vessel
                if (dockedParts.ContainsKey(p.flightID))
                {
                    otherVessel.Enqueue(p);
                    continue;
                }

                // Special handling of certain modules
                for (int i = 0; i < p.Modules.Count; i++)
                {
                    PartModule pm = p.Modules.GetModule(i);

                    // If this is a docking node, track the docked part
                    if (pm.moduleName == "ModuleDockingNode")
                    {
                        ModuleDockingNode dock = (ModuleDockingNode)pm;
                        if (dock.dockedPartUId != 0)
                        {
                            dockedParts[dock.dockedPartUId] = dock.dockedPartUId;
                        }
                    }
                    else if (pm.moduleName == "ModuleDecouple")
                    {
                        // Just assume all parts can decouple from this, it's easier and
                        // effectively the same thing
                        decoupler = true;
                        dockedParts[p.parent.flightID] = p.parent.flightID;
                        foreach (Part child in p.children)
                        {
                            dockedParts[child.flightID] = child.flightID;
                        }
                    }
                }

                // Go through our child parts
                foreach (Part child in p.children)
                {
                    if (!visited.ContainsKey(child))
                    {
                        queue.Enqueue(child);
                        visited[child] = 1;
                    }
                }

                // Confirm if parent part has been visited
                if (p.parent != null && !visited.ContainsKey(p.parent))
                {
                    queue.Enqueue(p.parent);
                    visited[p.parent] = 1;
                }

                // Add this part to the hash
                if (!decoupler)
                {
                    hash ^= p.flightID;
                }

                // We've processed this node
                visited[p] = 2;
            }

            // Add the last hash
            hashes.Add(hash);

            return hashes;
        }

        /*
         * Checks whether the given vessel meets the condition, and completes the contract parameter as necessary.
         */
        protected virtual void CheckVessel(Vessel vessel)
        {
            // No vessel to check.
            if (vessel == null)
            {
                return;
            }

            if (IsIgnoredVesselType(vessel.vesselType))
            {
                return;
            }

            // Using VesselParameterGroup logic
            if (Parent.GetType() == typeof(VesselParameterGroup))
            {
                // Set the craft specific state
                SetState(vessel, VesselMeetsCondition(vessel) ? Contracts.ParameterState.Complete :
                    Contracts.ParameterState.Incomplete);

                // Update the group
                VesselParameterGroup vpg = (VesselParameterGroup) Parent;
                vpg.UpdateState();

            }
            // Logic applies only to active vessel
            else if (vessel.isActiveVessel)
            {
                if (VesselMeetsCondition(vessel))
                {
                    SetComplete();
                }
                else
                {
                    SetIncomplete();
                }
            }
        }

        /*
         * Checks if this is one of the ignored vessel types.
         */
        public static bool IsIgnoredVesselType(VesselType vesselType) 
        {
            switch (vesselType)
            {
                case VesselType.Debris:
                case VesselType.EVA:
                case VesselType.Flag:
                case VesselType.SpaceObject:
                case VesselType.Unknown:
                    return true;
                default:
                    return false;
            }
        }

        protected abstract bool VesselMeetsCondition(Vessel vessel);
    }
}
