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
        private class VesselInfo
        {
            public Contracts.ParameterState state = ParameterState.Incomplete;
            public double completionTime = 0.0;
            public Vessel vessel = null;
        }
        private Dictionary<Guid, VesselInfo> vesselInfo;

        public VesselParameter()
            : base()
        {
            vesselInfo = new Dictionary<Guid, VesselInfo>();
        }

        protected override string GetHashString()
        {
            return (this.Root.MissionSeed.ToString() + this.Root.DateAccepted.ToString() + this.ID);
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            // Save vessel information
            foreach (KeyValuePair<Guid, VesselInfo> p in vesselInfo)
            {
                ConfigNode child = new ConfigNode("VESSEL_STATS");
                child.AddValue("vessel", p.Key);
                child.AddValue("state", p.Value.state);
                if (p.Value.state == ParameterState.Complete)
                {
                    child.AddValue("completionTime", p.Value.completionTime);
                }
                node.AddNode(child);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            // Load completion times
            foreach (ConfigNode child in node.GetNodes("VESSEL_STATS"))
            {
                Guid id = new Guid(child.GetValue("vessel"));
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == id);

                VesselInfo info = new VesselInfo();
                info.vessel = vessel;
                info.state = (ParameterState)Enum.Parse(typeof(ParameterState), child.GetValue("state"));
                if (state == ParameterState.Complete)
                {
                    info.completionTime = Convert.ToDouble(child.GetValue("completionTime"));
                }
                vesselInfo[id] = info;
            }
        }

        /*
         * Sets the parameter state for the given vessel.
         */
        protected virtual void SetState(Vessel vessel, Contracts.ParameterState state)
        {
            // Initialize
            if (!vesselInfo.ContainsKey(vessel.id))
            {
                vesselInfo[vessel.id] = new VesselInfo();
            }

            // Set the completion time
            if (state == Contracts.ParameterState.Complete &&
                vesselInfo[vessel.id].state != Contracts.ParameterState.Complete)
            {
                vesselInfo[vessel.id].completionTime = Planetarium.GetUniversalTime();
            }

            // Set the rest
            vesselInfo[vessel.id].state = state;
            vesselInfo[vessel.id].vessel = vessel;
        }

        /*
         * Sets the global parameter state to the one of the given vessel
         */
        public virtual void SetState(Vessel vessel)
        {
            if (vesselInfo.ContainsKey(vessel.id) && vesselInfo[vessel.id].state == ParameterState.Complete)
            {
                SetComplete();
            }
            else
            {
                SetIncomplete();
            }
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
            return vesselInfo.Where(p => p.Value.state == Contracts.ParameterState.Complete).Select(p => p.Value.vessel);
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
            GameEvents.onVesselCreate.Add(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
            GameEvents.onVesselCreate.Remove(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselCreateChange));
        }

        protected virtual void OnFlightReady()
        {
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected virtual void OnVesselCreateChange(Vessel vessel)
        {
            CheckVessel(vessel);
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

            // Ignored vessel types
            switch (vessel.vesselType)
            {
                case VesselType.Debris:
                case VesselType.EVA:
                case VesselType.Flag:
                case VesselType.SpaceObject:
                case VesselType.Unknown:
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

        protected abstract bool VesselMeetsCondition(Vessel vessel);
    }
}
