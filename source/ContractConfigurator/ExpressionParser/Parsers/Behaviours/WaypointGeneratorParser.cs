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

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<WaypointGeneratorFactory, List<Waypoint>>("Waypoints",
                wgf =>
                {
                    Debug.Log("wgf = " + wgf);
                    Debug.Log("wgf.Current = " + wgf.Current);
                    if (wgf.Current != null)
                    {
                        Debug.Log("wgf.Current.Waypoints() = " + wgf.Current.Waypoints());
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
            yield return "parameter";
            yield return "hidden";
            yield return "targetBody";
            yield return "icon";

            switch(type)
            {
                case "WAYPOINT":
                    yield return "latitude";
                    yield return "longitude";
                    break;
                case "RANDOM_WAYPOINT":
                case "RANDOM_WAYPOINT_NEAR":
                    yield return "count";
                    yield return "waterAllowed";
                    if (type == "RANDOM_WAYPOINT")
                    {
                        yield return "forceEquatorial";
                    }
                    else
                    {
                        yield return "nearIndex";
                        yield return "minDistance";
                        yield return "maxDistance";
                    }
                    break;
                case "PQS_CITY":
                    yield return "pqsCity";
                    yield return "pqsOffset";
                    break;
            }

        }
    }
}
