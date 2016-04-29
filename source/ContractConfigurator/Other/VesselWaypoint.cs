using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using FinePrint;

namespace ContractConfigurator
{
    public class VesselWaypoint
    {
        private class VesselWaypointRenderer : MonoBehaviour
        {
            public static VesselWaypointRenderer Instance;
            public List<VesselWaypoint> waypoints = new List<VesselWaypoint>();

            void Awake()
            {
                if (Instance != null)
                {
                    Destroy(Instance);
                }
                Instance = this;
            }

            void OnPreCull()
            {
                foreach (VesselWaypoint vw in waypoints)
                {
                    vw.UpdateWaypoint();
                }
            }
        }

        Contract contract;
        Waypoint waypoint;
        string vesselKey;

        public VesselWaypoint(Contract contract, string vesselKey)
        {
            this.contract = contract;
            this.vesselKey = vesselKey;
        }

        public void Register()
        {
            // Only register in scenes with a MapView
            if (MapView.fetch == null)
            {
                return;
            }

            // Register the orbit drawing class
            if (MapView.MapCamera.gameObject.GetComponent<VesselWaypointRenderer>() == null)
            {
                MapView.MapCamera.gameObject.AddComponent<VesselWaypointRenderer>();
            }
            VesselWaypointRenderer.Instance.waypoints.Add(this);
        }

        public void Unregister()
        {
            if (VesselWaypointRenderer.Instance != null)
            {
                VesselWaypointRenderer.Instance.waypoints.Remove(this);
                if (waypoint != null)
                {
                    WaypointManager.RemoveWaypoint(waypoint);
                }
            }
        }

        public void UpdateWaypoint()
        {
            // Get the vessel to use
            Vessel vessel = ContractVesselTracker.Instance.GetAssociatedVessel(vesselKey);
            if (vessel == null)
            {
                if (waypoint != null)
                {
                    WaypointManager.RemoveWaypoint(waypoint);
                    waypoint = null;
                }

                return;
            }

            if (waypoint == null)
            {
                waypoint = new Waypoint();
                waypoint.seed = contract.MissionSeed;
                waypoint.index = 0;
                waypoint.landLocked = false;
                waypoint.id = "vessel";
                waypoint.isNavigatable = false;
                waypoint.enableTooltip = false;
                waypoint.enableMarker = false;
                waypoint.contractReference = contract;
                WaypointManager.AddWaypoint(waypoint);
            }

            waypoint.name = vessel.loaded ? vessel.vesselName : vessel.protoVessel.vesselName;
            waypoint.celestialName = vessel.mainBody.GetName();
            waypoint.latitude = vessel.loaded ? vessel.latitude : vessel.protoVessel.latitude;
            waypoint.longitude = vessel.loaded ? vessel.longitude : vessel.protoVessel.longitude;
            waypoint.altitude = vessel.loaded ? vessel.altitude : vessel.protoVessel.altitude;

            Orbit orbit = vessel.loaded ? vessel.orbit : vessel.protoVessel.orbitSnapShot.Load();
            waypoint.SetFadeRange(orbit.ApR);
            waypoint.orbitPosition = orbit.getPositionAtUT(Planetarium.GetUniversalTime());
            waypoint.isOnSurface = vessel.LandedOrSplashed;
        }
    }
}
