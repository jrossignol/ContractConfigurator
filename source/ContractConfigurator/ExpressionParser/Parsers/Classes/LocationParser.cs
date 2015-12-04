using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Location.
    /// </summary>
    public class LocationParser : ClassExpressionParser<Location>, IExpressionParserRegistrer
    {
        private static System.Random random = new System.Random();

        static LocationParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Location), typeof(LocationParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<Location, double>("Latitude", location => location == null ? 0.0 : location.lat));
            RegisterMethod(new Method<Location, double>("Longitude", location => location == null ? 0.0 : location.lon));
            RegisterMethod(new Method<Location, Biome>("Biome", BiomeAtLocation));

            RegisterGlobalFunction(new Function<Location>("KSCLocation", KSCLocation));
        }

        private static Biome BiomeAtLocation(Location location)
        {
            if (location == null || location.body == null)
            {
                return null;
            }

            double latRads = location.lat * Math.PI / 180.0;
            double lonRads = location.lon * Math.PI / 180.0;
            string biome = location.body.BiomeMap.GetAtt(latRads, lonRads).name.Replace(" ", "");

            return new Biome(location.body, biome);
        }

        private static Location KSCLocation()
        {
            CelestialBody home = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).First();
            PQSCity ksc = home.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == "KSC").First();

            return new Location(home, home.GetLatitude(ksc.transform.position), home.GetLongitude(ksc.transform.position));
        }

        public LocationParser()
        {
        }
    }
}