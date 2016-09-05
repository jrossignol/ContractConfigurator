using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using FinePrint;
using FinePrint.Utilities;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for requiring a Kerbal to visit a waypoint.
    /// </summary>
    public class VisitWaypoint : VesselParameter
    {
        /// <summary>
        /// Child class for checking waypoints, because completed/disabled parameters don't get events.
        /// </summary>
        public class WaypointChecker
        {
            VisitWaypoint visitWaypoint;
            public WaypointChecker(VisitWaypoint vw)
            {
                visitWaypoint = vw;
                ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            }

            ~WaypointChecker()
            {
                ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            }

            protected void OnParameterChange(Contract c, ContractParameter p)
            {
                visitWaypoint.OnParameterChange(c, p);
            }
        }

        protected int waypointIndex { get; set; }
        protected Waypoint waypoint { get; set; }
        protected double distance { get; set; }
        protected bool hideOnCompletion { get; set; }
        protected bool showMessages { get; set; }
        
        private double height = double.MaxValue;
        private bool nearWaypoint = false;

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        private WaypointChecker waypointChecker;

        public VisitWaypoint()
            : base()
        {
            waypointChecker = new WaypointChecker(this);
        }

        public VisitWaypoint(int waypointIndex, double distance, bool hideOnCompletion, bool showMessages, string title)
            : base(title)
        {
            waypointChecker = new WaypointChecker(this);

            this.distance = distance;
            this.waypointIndex = waypointIndex;
            this.hideOnCompletion = hideOnCompletion;
            this.showMessages = showMessages;
        }

        protected override string GetParameterTitle()
        {
            if (waypoint == null && Root != null)
            {
                waypoint = FetchWaypoint(Root, true);
            }

            string output = title;
            if (string.IsNullOrEmpty(title) && waypoint != null)
            {
                if (waypoint.isOnSurface)
                {
                    output = "Location: " + waypoint.name;
                }
                else
                {
                    output = "Location: " + waypoint.altitude.ToString("N0") + "meters above " + waypoint.name;
                }
            }
            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("distance", distance);
            node.AddValue("waypointIndex", waypointIndex);
            node.AddValue("hideOnCompletion", hideOnCompletion);
            node.AddValue("showMessages", showMessages);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            distance = Convert.ToDouble(node.GetValue("distance"));
            waypointIndex = Convert.ToInt32(node.GetValue("waypointIndex"));
            hideOnCompletion = ConfigNodeUtil.ParseValue<bool?>(node, "hideOnCompletion", (bool?)true).Value;
            showMessages = ConfigNodeUtil.ParseValue<bool?>(node, "showMessages", (bool?)false).Value;
        }

        public void OnParameterChange(Contract c, ContractParameter p)
        {
            if (c != Root)
            {
                return;
            }

            // Hide the waypoint if we are done with it
            if (hideOnCompletion && waypoint != null && waypoint.visible)
            {
                for (IContractParameterHost paramHost = this; paramHost != Root; paramHost = paramHost.Parent)
                {
                    if (state == ParameterState.Complete)
                    {
                        ContractParameter param = paramHost as ContractParameter;
                        if (param != null && !param.Enabled)
                        {
                            waypoint.visible = false;
                            NavWaypoint navPoint = NavWaypoint.fetch;
                            if (navPoint != null && NavWaypoint.fetch.IsActive && navPoint.Latitude == waypoint.latitude && navPoint.Longitude == waypoint.longitude)
                            {
                                NavWaypoint.fetch.Clear();
                                NavWaypoint.fetch.Deactivate();
                            }
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
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
        public Waypoint FetchWaypoint(Contract c, bool silent = false)
        {
            // Find the WaypointGenerator behaviours
            IEnumerable<WaypointGenerator> waypointGenerators = ((ConfiguredContract)c).Behaviours.OfType<WaypointGenerator>();

            if (!waypointGenerators.Any())
            {
                LoggingUtil.LogError(this, "Could not find WaypointGenerator BEHAVIOUR to couple with VisitWaypoint PARAMETER.");
                return null;
            }

            waypoint = waypointGenerators.SelectMany(wg => wg.Waypoints()).ElementAtOrDefault(waypointIndex);
            if (waypoint == null)
            {
                LoggingUtil.LogError(this, "Couldn't find waypoint in WaypointGenerator behaviour(s) with index " + waypointIndex + ".");
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

            // Make sure we have a waypoint
            if (waypoint == null && Root != null)
            {
                waypoint = FetchWaypoint(Root, true);
            }
            if (waypoint == null)
            {
                return false;
            }

            // Not even close
            if (vessel.mainBody.name != waypoint.celestialName)
            {
                return false;
            }

            // Default distance
            if (distance == 0.0)
            {
                // Close to the surface
                if (waypoint.altitude < 25.0)
                {
                    distance = 500.0;
                }
                else
                {
                    distance = Math.Max(1000.0, waypoint.altitude / 5.0);
                }
            }

            // Calculate the distance
            double actualDistance = WaypointUtil.GetDistanceToWaypoint(vessel, waypoint, ref height);
            LoggingUtil.LogVerbose(this, "Distance to waypoint '" + waypoint.name + "': " + actualDistance);

            bool check = actualDistance <= distance;

            // Output the message for entering/leaving the waypoint area.
            if (showMessages)
            {
                if (check ^ nearWaypoint)
                {
                    nearWaypoint = check;
                    string waypointName = waypoint.name + (waypoint.isClustered ? " " + StringUtilities.IntegerToGreek(waypoint.index) : "");
                    string msg = "You are " + (nearWaypoint ? "entering " : "leaving ") + waypointName + ".";
                    ScreenMessages.PostScreenMessage(msg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

                NavWaypoint navWaypoint = NavWaypoint.fetch;
                if (navWaypoint != null && navWaypoint.Latitude == waypoint.latitude && navWaypoint.Longitude == waypoint.longitude)
                {
                    navWaypoint.IsBlinking = nearWaypoint;
                }
            }

            return check;
        }
    }
}
