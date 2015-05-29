using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public class Location
    {
        public CelestialBody body;
        public double lat;
        public double lon;

        public Location(CelestialBody body, double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
            this.body = body;
        }

        public override string ToString()
        {
            return "Location[" + (body == null ? "null" : body.name) + ", " + lat + ", " + lon + "]";
        }
    }
}
