using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using FinePrint;

namespace ContractConfigurator.Behaviour
{
    /*
     * Class for spawning a Kerbal.
     */
    public class WaypointGenerator : ContractBehaviour
    {
        private class WaypointData
        {
            public Waypoint waypoint = new Waypoint();
            public string type = null;

            public WaypointData()
            {
            }

            public WaypointData(string type)
            {
                this.type = type;
            }

            public WaypointData(WaypointData orig, Contract contract)
            {
                type = orig.type;
                waypoint.altitude = orig.waypoint.altitude;
                waypoint.celestialName = orig.waypoint.celestialName;
                waypoint.height = orig.waypoint.height;
                waypoint.id = orig.waypoint.id;
                waypoint.index = orig.waypoint.index;
                waypoint.isClustered = orig.waypoint.isClustered;
                waypoint.isExplored = orig.waypoint.isExplored;
                waypoint.isOnSurface = orig.waypoint.isOnSurface;
                waypoint.landLocked = orig.waypoint.landLocked;
                waypoint.latitude = orig.waypoint.latitude;
                waypoint.longitude = orig.waypoint.longitude;
                waypoint.name = orig.waypoint.name;

                SetContract(contract);
            }

            public void SetContract(Contract contract)
            {
                waypoint.contractReference = contract;
                waypoint.seed = contract.MissionSeed;
            }
        }
        private List<WaypointData> waypoints = new List<WaypointData>();
        
        public WaypointGenerator() {}

        /*
         * Copy constructor.
         */
        public WaypointGenerator(WaypointGenerator orig, Contract contract)
            : base()
        {
            foreach (WaypointData old in orig.waypoints)
            {
                // Copy waypoint data
                waypoints.Add(new WaypointData(old, contract));
            }
        }

        public static WaypointGenerator Create(ConfigNode configNode, CelestialBody defaultBody)
        {
            WaypointGenerator wpGenerator = new WaypointGenerator();

            foreach (string type in new string[] { "RANDOM_WAYPOINT" })
            {
                foreach (ConfigNode child in configNode.GetNodes(type))
                {
                    WaypointData wpData = new WaypointData(type);

                    // Get target body
                    wpData.waypoint.celestialName = (child.HasValue("targetBody") ?
                        ConfigNodeUtil.ParseCelestialBody(child, "targetBody") : defaultBody).name;

                    // Get other waypoint attributes
                    wpData.waypoint.name = child.GetValue("name");
                    wpData.waypoint.id = child.GetValue("icon");

                    // Add to the list
                    wpGenerator.waypoints.Add(wpData);
                }
            }

            return wpGenerator;
        }

        protected override void OnOffered()
        {
            System.Random random = new System.Random(contract.MissionSeed);

            foreach (WaypointData wpData in waypoints)
            {
                // Do type-specific waypoint handling
                if (wpData.type == "RANDOM_WAYPOINT")
                {
                    // Generate the position
                    Debug.Log("adding info for a random waypoint");
                    WaypointManager.ChooseRandomPosition(out wpData.waypoint.latitude, out wpData.waypoint.longitude, wpData.waypoint.celestialName, true, false, random);
                }

                CreateWayPoint(wpData.waypoint);
                Debug.Log("Generated a waypoint at (" + wpData.waypoint.latitude + ", " + wpData.waypoint.longitude + ") on " + wpData.waypoint.celestialName);
            }
        }

        protected override void OnAccepted()
        {
        }

        protected override void OnCancelled()
        {
            RemoveKerbals();
        }

        protected override void OnDeadlineExpired()
        {
            RemoveKerbals();
        }

        protected override void OnDeclined()
        {
            RemoveKerbals();
        }

        protected override void OnGenerateFailed()
        {
            RemoveKerbals();
        }

        protected override void OnOfferExpired()
        {
            RemoveKerbals();
        }

        protected override void OnWithdrawn()
        {
            RemoveKerbals();
        }

        private void RemoveKerbals()
        {
/*            foreach (KerbalData kerbal in kerbals)
            {
                HighLogic.CurrentGame.CrewRoster.Remove(kerbal.crewMember.name);
                kerbal.crewMember = null;
            }*/
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (ConfigNode child in configNode.GetNodes("WAYPOINT"))
            {
                // Read all the waypont data
                WaypointData wpData = new WaypointData();
                wpData.type = child.GetValue("type");
                wpData.waypoint.celestialName = child.GetValue("celestialName");
                wpData.waypoint.name = child.GetValue("name");
                wpData.waypoint.id = child.GetValue("icon");
                wpData.waypoint.latitude = Convert.ToDouble(child.GetValue("latitude"));
                wpData.waypoint.longitude = Convert.ToDouble(child.GetValue("longitude"));
                wpData.waypoint.altitude = Convert.ToDouble(child.GetValue("altitude"));
                wpData.SetContract(contract);

                // Create additional waypoint details
                CreateWayPoint(wpData.waypoint);

                // Add to the global list
                waypoints.Add(wpData);
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (WaypointData wpData in waypoints)
            {
                ConfigNode child = new ConfigNode("WAYPOINT");

                child.AddValue("type", wpData.type);
                child.AddValue("celestialName", wpData.waypoint.celestialName);
                child.AddValue("name", wpData.waypoint.name);
                child.AddValue("icon", wpData.waypoint.id);
                child.AddValue("latitude", wpData.waypoint.latitude);
                child.AddValue("longitude", wpData.waypoint.longitude);
                child.AddValue("altitude", wpData.waypoint.altitude);

                configNode.AddNode(child);
            }
        }

        private void CreateWayPoint(Waypoint waypoint)
        {
            Debug.Log("Creating a waypoint.");
            waypoint.altitude = 0.0;
            waypoint.isOnSurface = true;
            waypoint.index = 0;

            // TODO - needs to show in flight too
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                WaypointManager.AddWaypoint(waypoint);
        }
    }
}
