using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            RegisterMethod(new Method<Vessel, int>("CrewCount", v => v == null ? 0 : v.GetCrewCount()));
            RegisterMethod(new Method<Vessel, int>("CrewCapacity", v => v == null ? 0 : v.GetCrewCapacity()));
            RegisterMethod(new Method<Vessel, int>("EmptyCrewSpace", v => v == null ? 0 : (v.GetCrewCount() - v.GetCrewCapacity())));

            RegisterGlobalFunction(new Function<List<Vessel>>("AllVessels", () => FlightGlobals.Vessels, false));
        }

        public VesselParser()
        {
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
            // In parse mode we typically don't have a save game loaded, so
            // don't try to get a vessel.  Give the benefit of the
            // doubt and assume that it will be a valid vessel (ie. no exception)
            if (parseMode)
            {
                return null;
            }

            return ContractVesselTracker.Instance.GetAssociatedVessel(token.sval);
        }
    }
}
