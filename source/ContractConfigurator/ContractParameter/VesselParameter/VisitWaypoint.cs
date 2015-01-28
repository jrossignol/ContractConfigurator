using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using FinePrint;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for requiring a Kerbal to visit a waypoint.
    /// </summary>
    public class VisitWaypoint : VesselParameter
    {
        protected string title { get; set; }
        protected int waypointIndex { get; set; }
        protected Waypoint waypoint { get; set; }
        protected double distance { get; set; }
        private double height = double.MaxValue;

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public VisitWaypoint()
            : this(0, 0.0f, null)
        {
        }

        public VisitWaypoint(int waypointIndex, double distance, string title)
            : base()
        {
            this.title = title;
            this.distance = distance;
            this.waypointIndex = waypointIndex;
        }

        protected override string GetTitle()
        {
            if (string.IsNullOrEmpty(title) && waypoint != null)
            {
                if (waypoint.isOnSurface)
                {
                    title = "Location: " + waypoint.name;
                }
                else
                {
                    title = "Location: " + waypoint.altitude.ToString("N0") + "meters above " + waypoint.name;
                }
            }
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("distance", distance);
            node.AddValue("waypointIndex", waypointIndex);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            distance = Convert.ToDouble(node.GetValue("distance"));
            waypointIndex = Convert.ToInt32(node.GetValue("waypointIndex"));
        }

        /// <summary>
        /// Goes and finds the waypoint for our parameter.
        /// </summary>
        /// <returns>The waypoint used by our parameter.</returns>
        protected Waypoint FetchWaypoint()
        {
            return FetchWaypoint(Root);
        }

        /// <summary>
        /// Goes and finds the waypoint for our parameter.
        /// </summary>
        /// <param name="c">The contract</param>
        /// <returns>The waypoint used by our parameter.</returns>
        public Waypoint FetchWaypoint(Contract c)
        {
            // Get the WaypointGenerator behaviour
            WaypointGenerator waypointGenerator = ((ConfiguredContract)c).Behaviours.OfType<WaypointGenerator>().First<WaypointGenerator>();

            if (waypointGenerator == null)
            {
                LoggingUtil.LogError(this, "Could not find WaypointGenerator BEHAVIOUR to couple with VisitWaypoint PARAMETER.");
                return null;
            }

            // Get the waypoint
            try
            {
                waypoint = waypointGenerator.GetWaypoint(waypointIndex);
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Couldn't find waypoint in WaypointGenerator with index " + waypointIndex + ": " + e.Message);
            }

            return waypoint;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // We don't do this on load because parameters load before behaviours (due to stock logic)
            if (waypoint == null)
            {
                FetchWaypoint();
            }

            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                CheckVessel(FlightGlobals.ActiveVessel);
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            // Not even close
            if (vessel.mainBody.name != waypoint.celestialName)
            {
                return false;
            }

            // Default distance
            if (distance == 0.0)
            {
                if (waypoint.isOnSurface)
                {
                    distance = 500.0;
                }
                else
                {
                    distance = Math.Max(1000.0, waypoint.altitude / 5.0);
                }
            }

            // Calculate the distance
            double actualDistance = GetDistanceToWaypoint(vessel, waypoint);

            LoggingUtil.LogVerbose(this, "Distance to waypoint '" + waypoint.name + "': " + actualDistance);
            return actualDistance <= distance;
        }


        /// <summary>
        /// Gets the  distance in meters from the activeVessel to the given waypoint.
        /// </summary>
        /// <param name="wpd">Activated waypoint</param>
        /// <returns>Distance in meters</returns>
        protected double GetDistanceToWaypoint(Vessel vessel, Waypoint waypoint)
        {
            CelestialBody celestialBody = vessel.mainBody;

            // Figure out the terrain height
            if (height == double.MaxValue)
            {
                double latRads = Math.PI / 180.0 * waypoint.latitude;
                double lonRads = Math.PI / 180.0 * waypoint.longitude;
                Vector3d radialVector = new Vector3d(Math.Cos(latRads) * Math.Cos(lonRads), Math.Sin(latRads), Math.Cos(latRads) * Math.Sin(lonRads));
                height = Math.Max(celestialBody.pqsController.GetSurfaceHeight(radialVector) - celestialBody.pqsController.radius, 0.0);
            }

            // Use the haversine formula to calculate great circle distance.
            double sin1 = Math.Sin(Math.PI / 180.0 * (vessel.latitude - waypoint.latitude) / 2);
            double sin2 = Math.Sin(Math.PI / 180.0 * (vessel.longitude - waypoint.longitude) / 2);
            double cos1 = Math.Cos(Math.PI / 180.0 * waypoint.latitude);
            double cos2 = Math.Cos(Math.PI / 180.0 * vessel.latitude);

            double lateralDist = 2 * (celestialBody.Radius + height + waypoint.altitude) *
                Math.Asin(Math.Sqrt(sin1 * sin1 + cos1 * cos2 * sin2 * sin2));
            double heightDist = Math.Abs(waypoint.altitude + height - vessel.terrainAltitude);

            if (heightDist <= lateralDist / 2.0)
            {
                return lateralDist;
            }
            else
            {
                // Get the ratio to use in our formula
                double x = (heightDist - lateralDist / 2.0) / lateralDist;

                // x / (x + 1) starts at 0 when x = 0, and increases to 1
                return (x / (x + 1)) * heightDist + lateralDist;
            }
        }
    }
}
