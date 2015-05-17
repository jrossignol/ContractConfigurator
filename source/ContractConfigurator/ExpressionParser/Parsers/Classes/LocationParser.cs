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

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<Location, double>("Latitude", location => location == null ? 0.0 : location.lat));
            RegisterMethod(new Method<Location, double>("Longitude", location => location == null ? 0.0 : location.lon));
        }

        public LocationParser()
        {
        }
    }
}
