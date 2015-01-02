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
    /*
     * Parameter for requiring a Kerbal to visit a waypoint.
     */
    public class VisitWaypoint : VesselParameter
    {
        protected string title { get; set; }
        protected int waypointIndex { get; set; }
        protected Waypoint waypoint { get; set; }
        protected double distance { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

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

        /*
         * Goes and finds the waypoint for our parameter.
         */
        protected Waypoint FetchWaypoint()
        {
            return FetchWaypoint(Root);
        }

        /*
         * Goes and finds the waypoint for our parameter.
         */
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

        /*
         * Whether this vessel meets the parameter condition.
         */
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
            Vector3d waypointLocation = vessel.mainBody.GetRelSurfacePosition(waypoint.longitude, waypoint.latitude, waypoint.altitude);
            Vector3d vesselLocation = vessel.mainBody.GetRelSurfacePosition(vessel.longitude, vessel.latitude, vessel.altitude);
            double actualDistance = Vector3d.Distance(vesselLocation, waypointLocation);

            LoggingUtil.LogVerbose(this, "Distance to waypoint '" + waypoint.name + "': " + actualDistance);
            return actualDistance <= distance;
        }
    }
}
