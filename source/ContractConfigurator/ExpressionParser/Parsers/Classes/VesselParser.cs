using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Vessel.
    /// </summary>
    public class VesselParser : ClassExpressionParser<Vessel>, IExpressionParserRegistrer
    {
        static VesselParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Vessel), typeof(VesselParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<Vessel, bool>("IsLanded", v => v != null && v.Landed));
            RegisterMethod(new Method<Vessel, bool>("IsSplashed", v => v != null && v.Splashed));
            RegisterMethod(new Method<Vessel, bool>("IsOrbiting", v => v != null && !v.LandedOrSplashed));

            RegisterMethod(new Method<Vessel, List<ProtoCrewMember>>("Crew", v => v == null ? new List<ProtoCrewMember>() : v.GetVesselCrew()));
            RegisterMethod(new Method<Vessel, List<Part>>("Parts", v => v == null ? new List<Part>() : v.parts));

            RegisterMethod(new Method<Vessel, CelestialBody>("CelestialBody", v => v == null ? null : v.mainBody));
            RegisterMethod(new Method<Vessel, VesselType>("VesselType", v => v == null ? VesselType.Unknown : v.vesselType));

            RegisterMethod(new Method<Vessel, double>("Altitude", v => v == null ? 0.0 : v.altitude));

            RegisterMethod(new Method<Vessel, int>("CrewCount", GetCrewCount));
            RegisterMethod(new Method<Vessel, int>("CrewCapacity", GetCrewCapacity));
            RegisterMethod(new Method<Vessel, int>("EmptyCrewSpace", v => GetCrewCapacity(v) - GetCrewCount(v)));
            RegisterMethod(new Method<Vessel, int>("FreeDockingPorts", FreeDockingPorts));

            RegisterGlobalFunction(new Function<List<Vessel>>("AllVessels", () => FlightGlobals.Vessels, false));
        }

        public VesselParser()
        {
        }

        /// <summary>
        /// Gets the crew count, as the methods provided on vessel only return valid values for
        /// loaded vessels.
        /// </summary>
        /// <param name="v">The vessel to check.</param>
        /// <returns>The number of crew members on board</returns>
        static int GetCrewCount(Vessel v)
        {
            if (v == null)
            {
                return 0;
            }

            return v.protoVessel.protoPartSnapshots.Sum(pps => pps.protoModuleCrew.Count);
        }

        /// <summary>
        /// Gets the crew capacity, as the methods provided on vessel only return valid values for
        /// loaded vessels.
        /// </summary>
        /// <param name="v">The vessel to check.</param>
        /// <returns>The number of crew space</returns>
        static int GetCrewCapacity(Vessel v)
        {
            if (v == null)
            {
                return 0;
            }

            return v.protoVessel.protoPartSnapshots.Sum(pps => pps.partInfo.partPrefab.CrewCapacity);
        }

        /// <summary>
        /// Gets the number of free docking ports
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static int FreeDockingPorts(Vessel v)
        {
            if (v == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ProtoPartSnapshot pps in v.protoVessel.protoPartSnapshots)
            {
                foreach (ProtoPartModuleSnapshot ppms in pps.modules)
                {
                    if (ConfigNodeUtil.ParseValue<string>(ppms.moduleValues, "state", "") == "Ready")
                    {
                        count += 1;
                    }
                }
            }
            return count;
        }

        internal override U ConvertType<U>(Vessel value)
        {
            if (typeof(U) == typeof(string))
            {
                if (!parseMode)
                {
                    ContractVesselTracker.Instance.AssociateVessel(value.vesselName, value);
                }
                return (U)(object)value.vesselName;
            }
            return base.ConvertType<U>(value);
        }

        internal override Vessel ParseIdentifier(Token token)
        {
            // Try to parse more, as vessel names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[A-Za-z][\w\d]*)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            // In parse mode we typically don't have a save game loaded, so
            // don't try to get a vessel.  Give the benefit of the
            // doubt and assume that it will be a valid vessel (ie. no exception)
            if (parseMode)
            {
                return null;
            }
            Debug.Log("parsing vessel for identifier '" + identifier + "'");
            Debug.Log("result = " + ContractVesselTracker.Instance.GetAssociatedVessel(identifier));

            return ContractVesselTracker.Instance.GetAssociatedVessel(identifier);
        }
    }
}
