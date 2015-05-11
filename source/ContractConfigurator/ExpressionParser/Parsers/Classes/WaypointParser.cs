using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using FinePrint;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Waypoints.
    /// </summary>
    public class WaypointParser : ClassExpressionParser<Waypoint>, IExpressionParserRegistrer
    {
        static WaypointParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Waypoint), typeof(WaypointParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<Waypoint, string>("Name", w => w == null ? "" : w.name));
            RegisterMethod(new Method<Waypoint, double>("Latitude", w => w == null ? 0.0 : w.latitude));
            RegisterMethod(new Method<Waypoint, double>("Longitude", w => w == null ? 0.0 : w.longitude));
        }

        public WaypointParser()
        {
        }

        internal override U ConvertType<U>(Waypoint value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.name);
            }
            return base.ConvertType<U>(value);
        }
    }
}
