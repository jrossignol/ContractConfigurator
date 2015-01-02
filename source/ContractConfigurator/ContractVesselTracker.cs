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
        public static ContractVesselTracker Instance { get; private set; }
        public static EventData<GameEvents.HostTargetAction<Vessel, string>> OnVesselAssociation = new EventData<GameEvents.HostTargetAction<Vessel, string>>("OnVesselAssociation");
        public static EventData<GameEvents.HostTargetAction<Vessel, string>> OnVesselDisassociation = new EventData<GameEvents.HostTargetAction<Vessel, string>>("OnVesselDisassociation");

        private Dictionary<string, Guid> vessels = new Dictionary<string, Guid>();

        public ContractVesselTracker()
        {
            Instance = this;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (ConfigNode child in node.GetNodes("VESSEL"))
            {
                string key = child.GetValue("key");
                Guid id = new Guid(child.GetValue("id"));
                vessels[key] = id;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            foreach (KeyValuePair<string, Guid> p in vessels)
            {
                ConfigNode child = new ConfigNode("VESSEL");
                child.AddValue("key", p.Key);
                child.AddValue("id", p.Value);
                node.AddNode(child);
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
                Guid oldVesselId = vessels[key];
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
                vessels[key] = vessel.id;
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
                return FlightGlobals.Vessels.Find(v => v.id == vessels[key]);
            }
            return null;
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
