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
    public class VesselIdentifierParser : ClassExpressionParser<VesselIdentifier>, IExpressionParserRegistrer
    {
        static VesselIdentifierParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(VesselIdentifier), typeof(VesselIdentifierParser));
        }

        internal static void RegisterMethods()
        {
/*            RegisterMethod(new Method<Vessel, bool>("IsLanded", v => v != null && v.Landed));
            RegisterMethod(new Method<Vessel, bool>("IsSplashed", v => v != null && v.Splashed));
            RegisterMethod(new Method<Vessel, bool>("IsOrbiting", v => v != null && !v.LandedOrSplashed));

            RegisterMethod(new Method<Vessel, List<Kerbal>>("Crew", v => v == null ? new List<Kerbal>() : v.GetVesselCrew().
                Select<ProtoCrewMember, Kerbal>(pcm => new Kerbal(pcm)).ToList()));
            RegisterMethod(new Method<Vessel, List<AvailablePart>>("Parts", v => v == null ? new List<AvailablePart>() :
                v.parts.Select<Part, AvailablePart>(p => p.protoPartSnapshot.partInfo).ToList()));

            RegisterMethod(new Method<Vessel, CelestialBody>("CelestialBody", v => v == null ? null : v.mainBody));
            RegisterMethod(new Method<Vessel, VesselType>("VesselType", v => v == null ? VesselType.Unknown : v.vesselType));

            RegisterMethod(new Method<Vessel, double>("Altitude", v => v == null ? 0.0 : v.altitude));

            RegisterMethod(new Method<Vessel, int>("CrewCount", GetCrewCount));
            RegisterMethod(new Method<Vessel, int>("CrewCapacity", GetCrewCapacity));
            RegisterMethod(new Method<Vessel, int>("EmptyCrewSpace", v => GetCrewCapacity(v) - GetCrewCount(v)));
            RegisterMethod(new Method<Vessel, int>("FreeDockingPorts", FreeDockingPorts));
            RegisterMethod(new Method<Vessel, Resource, double>("ResourceQuantity", (v, r) => v == null || r == null ? 0.0 : v.ResourceQuantity(r.res)));

            RegisterGlobalFunction(new Function<List<Vessel>>("AllVessels", () => FlightGlobals.Vessels, false));*/
            RegisterGlobalFunction(new Function<VesselIdentifier, VesselIdentifier>("VesselIdentifier", v => v));
        }

        public VesselIdentifierParser()
        {
        }

        internal override U ConvertType<U>(VesselIdentifier value)
        {
            if (typeof(U) == typeof(Vessel))
            {
                if (!parseMode)
                {
                    return (U)(object)ContractVesselTracker.Instance.GetAssociatedVessel(value.identifier);
                }
                return (U)(object)null;
            }
            else if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? null : value.identifier);
            }
            return base.ConvertType<U>(value);
        }

        internal override VesselIdentifier ParseIdentifier(Token token)
        {
            // Try to parse more, as vessel names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[A-Za-z][\w\d]*)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            return new VesselIdentifier(identifier);
        }
    }
}
