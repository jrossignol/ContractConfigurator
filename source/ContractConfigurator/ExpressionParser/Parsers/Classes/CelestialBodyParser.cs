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
    /// Expression parser subclass for CelestialBody.
    /// </summary>
    public class CelestialBodyParser : ClassExpressionParser<CelestialBody>, IExpressionParserRegistrer
    {
        private enum CelestialBodyType
        {
            NOT_APPLICABLE,
            SUN,
            PLANET,
            MOON
        }
        private const double BARYCENTER_THRESHOLD = 100;

        private enum ProgressItem
        {
            REACHED,
            ORBITED,
            LANDED,
            ESCAPED,
            RETURNED_FROM
        }

        static CelestialBodyParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(CelestialBody), typeof(CelestialBodyParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<CelestialBody, bool>("HasAtmosphere", cb => cb != null && cb.atmosphere));
            RegisterMethod(new Method<CelestialBody, bool>("HasOcean", cb => cb != null && cb.ocean));
            RegisterMethod(new Method<CelestialBody, bool>("HasSurface", cb => cb != null && cb.pqsController != null));
            RegisterMethod(new Method<CelestialBody, bool>("IsHomeWorld", cb => cb != null && cb.isHomeWorld));
            RegisterMethod(new Method<CelestialBody, bool>("IsSun", cb => BodyType(cb) == CelestialBodyType.SUN));
            RegisterMethod(new Method<CelestialBody, bool>("IsPlanet", cb => BodyType(cb) == CelestialBodyType.PLANET));
            RegisterMethod(new Method<CelestialBody, bool>("IsMoon", cb => BodyType(cb) == CelestialBodyType.MOON));
            RegisterMethod(new Method<CelestialBody, bool>("IsOrbitalSurveyComplete", cb => cb != null && ResourceScenario.Instance != null &&
                ResourceScenario.Instance.gameSettings.GetPlanetScanInfo().Where(psd => psd.PlanetId == cb.flightGlobalsIndex).Any(), false));

            RegisterMethod(new Method<CelestialBody, bool>("HaveReached", cb => IsReached(cb, ProgressItem.REACHED), false));
            RegisterMethod(new Method<CelestialBody, bool>("HaveOrbited", cb => IsReached(cb, ProgressItem.ORBITED), false));
            RegisterMethod(new Method<CelestialBody, bool>("HaveLandedOn", cb => IsReached(cb, ProgressItem.LANDED), false));
            RegisterMethod(new Method<CelestialBody, bool>("HaveEscaped", cb => IsReached(cb, ProgressItem.ESCAPED), false));
            RegisterMethod(new Method<CelestialBody, bool>("HaveReturnedFrom", cb => IsReached(cb, ProgressItem.RETURNED_FROM), false));
            
            RegisterMethod(new Method<CelestialBody, double>("Radius", cb => cb != null ? cb.Radius : 0.0));
            RegisterMethod(new Method<CelestialBody, double>("Mass", cb => cb != null ? cb.Mass : 0.0));
            RegisterMethod(new Method<CelestialBody, double>("RotationalPeriod", cb => cb != null ? cb.rotationPeriod : 0.0));
            RegisterMethod(new Method<CelestialBody, double>("AtmosphereAltitude", cb => cb != null ? cb.atmosphereDepth : 0.0));
            RegisterMethod(new Method<CelestialBody, float>("FlyingAltitudeThreshold", cb => cb != null ? cb.scienceValues.flyingAltitudeThreshold : 0.0f));
            RegisterMethod(new Method<CelestialBody, float>("SpaceAltitudeThreshold", cb => cb != null ? cb.scienceValues.spaceAltitudeThreshold : 0.0f));
            RegisterMethod(new Method<CelestialBody, double>("SphereOfInfluence", cb => cb != null ? cb.sphereOfInfluence : 0.0));
            RegisterMethod(new Method<CelestialBody, double>("SemiMajorAxis", cb => cb != null && cb.orbit != null ? cb.orbit.semiMajorAxis : 0.0));

            RegisterMethod(new Method<CelestialBody, CelestialBody>("Parent", cb => cb != null ? cb.referenceBody : null));
            RegisterMethod(new Method<CelestialBody, List<CelestialBody>>("Children", cb => cb != null ? cb.orbitingBodies.ToList() : new List<CelestialBody>()));

            RegisterMethod(new Method<CelestialBody, List<Biome>>("Biomes", cb => cb != null && cb.BiomeMap != null ?
                cb.BiomeMap.Attributes.Select(att => new Biome(cb, att.name)).ToList() : new List<Biome>()));

            RegisterMethod(new Method<CelestialBody, string>("Name", cb => cb != null ? cb.name : ""));

            RegisterMethod(new Method<CelestialBody, double>("Multiplier", cb => cb != null ? GameVariables.Instance.GetContractDestinationWeight(cb) : 1.0));

            RegisterMethod(new Method<CelestialBody, double>("RemoteTechCoverage", cb => cb != null ? RemoteTechCoverage(cb) : 0.0d, false));
            RegisterMethod(new Method<CelestialBody, string, double>("SCANsatCoverage", SCANsatCoverage, false));

            RegisterGlobalFunction(new Function<CelestialBody>("HomeWorld", () => FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).First()));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("AllBodies", () => FlightGlobals.Bodies.Where(cb => cb != null && cb.Radius >= BARYCENTER_THRESHOLD).ToList()));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("OrbitedBodies", () => BodiesForItem(ProgressItem.ORBITED).ToList(), false));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("LandedBodies", () => BodiesForItem(ProgressItem.LANDED).ToList(), false));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("EscapedBodies", () => BodiesForItem(ProgressItem.ESCAPED).ToList(), false));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("ReachedBodies", () => BodiesForItem(ProgressItem.REACHED).ToList(), false));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("ReturnedFromBodies", () => BodiesForItem(ProgressItem.RETURNED_FROM).ToList(), false));
            RegisterGlobalFunction(new Function<CelestialBody, CelestialBody>("CelestialBody", cb => cb));
        }

        public CelestialBodyParser()
        {
        }

        public override U ConvertType<U>(CelestialBody value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.theName;
            }
            return base.ConvertType<U>(value);
        }

        private static IEnumerable<CelestialBody> BodiesForItem(ProgressItem pi)
        {
            if (ProgressTracking.Instance == null)
            {
                return Enumerable.Empty<CelestialBody>();
            }

            return ProgressTracking.Instance.celestialBodyNodes.Where(node => CheckTree(node, pi)).Select(node => node.Body).
                Where(cb => cb.Radius >= BARYCENTER_THRESHOLD);
        }

        private static bool IsReached(CelestialBody cb, ProgressItem pi)
        {
            if (ProgressTracking.Instance == null)
            {
                return false;
            }

            CelestialBodySubtree tree = ProgressTracking.Instance.celestialBodyNodes.Where(node => node.Body == cb).FirstOrDefault();
            return tree == null ? false : CheckTree(tree, pi);
        }

        private static bool CheckTree(CelestialBodySubtree tree, ProgressItem pi)
        {
            switch (pi)
            {
                case ProgressItem.REACHED:
                    return tree.IsReached;
                case ProgressItem.ORBITED:
                    return tree.orbit.IsComplete;
                case ProgressItem.LANDED:
                    return tree.landing.IsComplete;
                case ProgressItem.ESCAPED:
                    return tree.escape.IsComplete;
                case ProgressItem.RETURNED_FROM:
                    return tree.returnFromFlyby.IsComplete;
            }

            return false;
        }

        private static double SCANsatCoverage(CelestialBody cb, string scanType)
        {
            // Verify the SCANsat version
            if (!SCANsatUtil.VerifySCANsatVersion())
            {
                return 100.0;
            }

            // Verify the input
            if (cb == null)
            {
                return 100.0;
            }
            SCANsatUtil.ValidateSCANname(scanType);

            return SCANsatUtil.GetCoverage(SCANsatUtil.GetSCANtype(scanType), cb);
        }

        private static double RemoteTechCoverage(CelestialBody cb)
        {
            if (!Util.Version.VerifyRemoteTechVersion())
            {
                return 1.0;
            }

            Type rtProgressTracker = Util.Version.CC_RemoteTechAssembly.GetType("ContractConfigurator.RemoteTech.RemoteTechProgressTracker");

            // Get and invoke the method
            MethodInfo methodGetCoverage = rtProgressTracker.GetMethod("GetCoverage");
            return (double)methodGetCoverage.Invoke(null, new object[] { cb });
        }

        private static CelestialBodyType BodyType(CelestialBody cb)
        {
            if (cb == null || cb.Radius < BARYCENTER_THRESHOLD)
            {
                return CelestialBodyType.NOT_APPLICABLE;
            }

            CelestialBody sun = FlightGlobals.Bodies[0];
            if (cb == sun)
            {
                return CelestialBodyType.SUN;
            }

            // Add a special case for barycenters (Sigma binary)
            if (cb.referenceBody == sun || cb.referenceBody.Radius < BARYCENTER_THRESHOLD)
            {
                return CelestialBodyType.PLANET;
            }

            return CelestialBodyType.MOON;
        }


        public override CelestialBody ParseIdentifier(Token token)
        {
            // Try to parse more, as celestibla body names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[\w\d]+)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }
            return ConfigNodeUtil.ParseCelestialBodyValue(identifier);
        }
    }
}
