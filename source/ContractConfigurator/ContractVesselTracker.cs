using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for tracking associations between a name (key) and a vessel.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    public class ContractVesselTracker : ScenarioModule
    {
        private class VesselInfo
        {
            public Guid id;
            public uint hash;

            public VesselInfo(Guid id, uint hash)
            {
                this.id = id;
                this.hash = hash;
            }

            public VesselInfo(Vessel v)
            {
                this.id = v.id;
                this.hash = v.GetHashes().First();
            }
        }

        public static ContractVesselTracker Instance { get; private set; }
        public static EventData<GameEvents.HostTargetAction<Vessel, string>> OnVesselAssociation = new EventData<GameEvents.HostTargetAction<Vessel, string>>("OnVesselAssociation");
        public static EventData<GameEvents.HostTargetAction<Vessel, string>> OnVesselDisassociation = new EventData<GameEvents.HostTargetAction<Vessel, string>>("OnVesselDisassociation");

        private Dictionary<string, VesselInfo> vessels = new Dictionary<string, VesselInfo>();
        private Vessel lastBreak = null;

        public ContractVesselTracker()
        {
            Instance = this;
        }

        public void Start()
        {
            GameEvents.onPartJointBreak.Add(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));
            GameEvents.onVesselDestroy.Add(new EventData<Vessel>.OnEvent(OnVesselDestroy));
        }

        public void OnDestroy()
        {
            GameEvents.onPartJointBreak.Remove(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
            GameEvents.onVesselWasModified.Remove(new EventData<Vessel>.OnEvent(OnVesselWasModified));
            GameEvents.onVesselDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselDestroy));
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (ConfigNode child in node.GetNodes("VESSEL"))
            {
                string key = child.GetValue("key");
                Guid id = new Guid(child.GetValue("id"));
                uint hash = ConfigNodeUtil.ParseValue<uint>(child, "hash", 0);

                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == id);
                if (vessel == null)
                {
                    id = Guid.Empty;
                }
                else if (hash == 0 && HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    hash = vessel.GetHashes().First();
                }

                if (id != Guid.Empty)
                {
                    vessels[key] = new VesselInfo(id, hash);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            foreach (KeyValuePair<string, VesselInfo> p in vessels)
            {
                VesselInfo vi = p.Value;

                // First find the vessel by id
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == vi.id);

                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    // If not found, attempt to find it by hash
                    if (vessel == null)
                    {
                        vessel = FlightGlobals.Vessels.Find(v => v.GetHashes().Contains(vi.hash));
                    }
                    // If found, verify the hash
                    else
                    {
                        IEnumerable<uint> hashes = vessel.GetHashes();
                        if (!hashes.Contains(vi.hash))
                        {
                            vi.hash = hashes.FirstOrDefault();
                        }
                    }

                }

                if (vessel != null)
                {
                    ConfigNode child = new ConfigNode("VESSEL");
                    child.AddValue("key", p.Key);
                    child.AddValue("id", vi.id);
                    child.AddValue("hash", vi.hash);
                    node.AddNode(child);
                }
            }
        }

        protected virtual void OnPartJointBreak(PartJoint p)
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                return;
            }

            if (GetAssociatedKeys(p.Parent.vessel).Any())
            {
                lastBreak = p.Parent.vessel;
            }
        }

        protected virtual void OnVesselWasModified(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "OnVesselWasModified: " + vessel.id);

            // Check for a vessel creation after a part joint break
            if (HighLogic.LoadedScene != GameScenes.FLIGHT || lastBreak == null || vessel == lastBreak)
            {
                return;
            }

            IEnumerable<uint> hashes = vessel.GetHashes();

            // OnVesselWasModified gets called twice, on the first call the vessels are still
            // connected.  Check for that case.
            if (hashes.First() == lastBreak.GetHashes().First())
            {
                // The second call will be for the original vessel.  Swap over to check that one.
                lastBreak = vessel;
                return;
            }

            foreach (string key in GetAssociatedKeys(lastBreak))
            {
                // Check if we need to switch over to the newly created vessel
                VesselInfo vi = vessels[key];
                if (hashes.Contains(vi.hash))
                {
                    vi.id = vessel.id;
                    OnVesselAssociation.Fire(new GameEvents.HostTargetAction<Vessel, string>(vessel, key));
                }
            }

            lastBreak = null;
        }

        protected virtual void OnVesselDestroy(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "OnVesselDestroy " + vessel.id);

            // Try to change any associations over if this is due to a docking event
            foreach (string key in GetAssociatedKeys(vessel).ToList())
            {
                // Check if we need to switch over to the newly created vessel
                VesselInfo vi = vessels[key];
                Vessel newVessel = FlightGlobals.Vessels.Find(v => v != vessel && v.GetHashes().Contains(vi.hash));
                if (newVessel != null)
                {
                    Debug.Log("switch association over to " + newVessel.id);
                    vi.id = newVessel.id;
                }
                else
                {
                    AssociateVessel(key, null);
                }
            }
        }

        /// <summary>
        /// Creates a permanent link between the given vessel and key.
        /// </summary>
        /// <param name="key">The key to create an association with.</param>
        /// <param name="vessel">The vessel that will be associated with the key</param>
        public void AssociateVessel(string key, Vessel vessel)
        {
            if (vessel != null)
            {
                LoggingUtil.LogVerbose(this, "Associating vessel " + vessel.id + " with key '" + key + "'.");
            }
            else
            {
                LoggingUtil.LogVerbose(this, "Disassociating key '" + key + "'.");
            }

            // First remove whatever was there
            if (vessels.ContainsKey(key))
            {
                Guid oldVesselId = vessels[key].id;
                Vessel oldVessel = FlightGlobals.Vessels.Find(v => v.id == oldVesselId);
                vessels.Remove(key);

                if (oldVessel != null)
                {
                    LoggingUtil.LogVerbose(this, "Firing OnVesselDisassociation.");
                    OnVesselDisassociation.Fire(new GameEvents.HostTargetAction<Vessel, string>(oldVessel, key));
                }
            }

            // Add the new vessel
            if (vessel != null)
            {
                vessels[key] = new VesselInfo(vessel);
                LoggingUtil.LogVerbose(this, "Firing OnVesselAssociation.");
                OnVesselAssociation.Fire(new GameEvents.HostTargetAction<Vessel, string>(vessel, key));
            }
        }

        /// <summary>
        /// Gets the vessel associated with the given key.
        /// </summary>
        /// <param name="key">The key to find an associated vessel for.</param>
        /// <returns>The vessel that is associated to the given key or null if none.</returns>
        public Vessel GetAssociatedVessel(string key)
        {
            if (vessels.ContainsKey(key))
            {
                return FlightGlobals.Vessels.Find(v => v.id == vessels[key].id);
            }
            return null;
        }

        /// <summary>
        /// Gets all the keys associated to the given vessel.
        /// </summary>
        /// <param name="v">The vessel to check</param>
        /// <returns>And enumeration of all keys</returns>
        public IEnumerable<string> GetAssociatedKeys(Vessel v)
        {
            foreach (string key in vessels.Where(p => p.Value.id == v.id).Select(p => p.Key))
            {
                yield return key;
            }
        }

        /// <summary>
        /// Gets the name that should be displayed to players for the given key.
        /// </summary>
        /// <param name="key">The key to check for.</param>
        /// <returns>The vessel name if there is a vessel associated with this key.  The key otherwise.</returns>
        public string GetDisplayName(string key)
        {
            Vessel v = GetAssociatedVessel(key);
            return v == null ? key : v.vesselName;
        }
    }
}
