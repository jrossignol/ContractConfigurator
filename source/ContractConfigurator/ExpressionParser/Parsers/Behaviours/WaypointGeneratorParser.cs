using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ContractConfigurator.Behaviour;
using FinePrint;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for WaypointGenerator behaviour.
    /// </summary>
    public class WaypointGeneratorParser : BehaviourParser<WaypointGeneratorFactory>, IExpressionParserRegistrer
    {
        static WaypointGeneratorParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(WaypointGeneratorFactory), typeof(WaypointGeneratorParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<WaypointGeneratorFactory, List<Waypoint>>("Waypoints",
                wgf => wgf.Current != null ? wgf.Current.Waypoints().ToList() : new List<Waypoint>(), false));
        }

        public WaypointGeneratorParser()
        {
        }
    }
}
