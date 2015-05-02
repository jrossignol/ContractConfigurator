using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FinePrint;

namespace ContractConfigurator
{
    public static class WaypointUtil
    {
        /// <summary>
        /// Gets the  distance in meters from the activeVessel to the given waypoint.
        /// </summary>
        /// <param name="wpd">Activated waypoint</param>
        /// <returns>Distance in meters</returns>
        public static double GetDistanceToWaypoint(Vessel vessel, Waypoint waypoint, ref double height)
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
            double heightDist = Math.Abs(waypoint.altitude + height - vessel.altitude);

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

        /// <summary>
        /// Gets the  distance in meters between two points
        /// </summary>
        /// <param name="lat1">First latitude</param>
        /// <param name="lon1">First longitude</param>
        /// <param name="lat2">Second latitude</param>
        /// <param name="lon2">Second longitude</param>
        /// <returns>the distance</returns>
        public static double GetDistance(double lat1, double lon1, double lat2, double lon2, double altitude)
        {
            // Use the haversine formula to calculate great circle distance.
            double sin1 = Math.Sin(Math.PI / 180.0 * (lat1 - lat2) / 2);
            double sin2 = Math.Sin(Math.PI / 180.0 * (lon1 - lon2) / 2);
            double cos1 = Math.Cos(Math.PI / 180.0 * lat2);
            double cos2 = Math.Cos(Math.PI / 180.0 * lat1);

            return  2 * (altitude) * Math.Asin(Math.Sqrt(sin1 * sin1 + cos1 * cos2 * sin2 * sin2));
        }
    }
}
