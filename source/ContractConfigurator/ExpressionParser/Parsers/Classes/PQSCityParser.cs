using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSPAchievements;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for PQSCity.
    /// </summary>
    public class PQSCityParser : ClassExpressionParser<PQSCity>, IExpressionParserRegistrer
    {
        static PQSCityParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(PQSCity), typeof(PQSCityParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<PQSCity, Location>("Location", GetLocation, false));
            RegisterMethod(new Method<PQSCity, string>("Name", city => city != null ? city.name : null));
            RegisterMethod(new Method<PQSCity, CelestialBody>("CelestialBody", city => city != null ? city.celestialBody : null));

            RegisterGlobalFunction(new Function<PQSCity>("KSC", () => FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).First().GetComponentsInChildren<PQSCity>(true).Where(city => city.name == "KSC").First()));
        }

        public PQSCityParser()
        {
        }

        public override U ConvertType<U>(PQSCity value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.name;
            }
            return base.ConvertType<U>(value);
        }

        public static Location GetLocation(PQSCity city)
        {
            if (city == null)
            {
                return null;
            }

            Vector3d position = city.transform.position;
            CelestialBody body = Part.GetComponentUpwards<CelestialBody>(city.sphere.gameObject);

            return new Location(body, body.GetLatitude(position), body.GetLongitude(position));
        }
    }
}
