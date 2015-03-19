using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public static class LocationUtil
    {
        public static double TerrainHeight(double latitude, double longitude, CelestialBody body)
        {
            // Not sure when this happens - for Sun and Jool?
            if (body.pqsController == null)
            {
                return 0;
            }

            // Figure out the terrain height
            double latRads = Math.PI / 180.0 * latitude;
            double lonRads = Math.PI / 180.0 * longitude;
            Vector3d radialVector = new Vector3d(Math.Cos(latRads) * Math.Cos(lonRads), Math.Sin(latRads), Math.Cos(latRads) * Math.Sin(lonRads));
            return Math.Max(body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius, 0.0);
        }
    }
}
