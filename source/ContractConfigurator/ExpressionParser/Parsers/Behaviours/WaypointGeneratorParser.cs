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
                wgf =>
                {
                    if (wgf.Current != null)
                    {
                        return wgf.Current.Waypoints().ToList();
                    }
                    else
                    {
                        CheckInitialized(wgf);
                        return new List<Waypoint>();
                    }
                }, false));
        }

        public WaypointGeneratorParser()
        {
        }

        protected static void CheckInitialized(WaypointGeneratorFactory wgf)
        {
            foreach (DataNode dataNode in wgf.dataNode.Children)
            {
                foreach (string identifier in GetIdentifiers((string)dataNode["type"]))
                {
                    if (!dataNode.IsInitialized(identifier))
                    {
                        throw new DataNode.ValueNotInitialized(dataNode.Path() + identifier);
                    }
                }
            }
        }

        protected static IEnumerable<string> GetIdentifiers(string type)
        {
            yield return "name";
            yield return "altitude";

            if (type == "WAYPOINT")
            {
                yield return "latitude";
                yield return "longitude";
            }
        }
    }
}
