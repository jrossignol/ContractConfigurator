using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator
{
    public class Location
    {
        public double lat;
        public double lon;

        public Location(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }

        public override string ToString()
        {
            return "Location[" + lat + ", " + lon + "]";
        }
    }
}
