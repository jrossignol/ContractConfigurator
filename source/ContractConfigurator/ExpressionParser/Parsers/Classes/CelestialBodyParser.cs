using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSPAchievements;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for CelestialBody.
    /// </summary>
    public class CelestialBodyParser : ClassExpressionParser<CelestialBody>, IExpressionParserRegistrer
    {
        static CelestialBodyParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(CelestialBody), typeof(CelestialBodyParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<CelestialBody, bool>("HasAtmosphere", cb => cb != null && cb.atmosphere));
            RegisterMethod(new Method<CelestialBody, bool>("HasOcean", cb => cb != null && cb.ocean));
            RegisterMethod(new Method<CelestialBody, bool>("HasSurface", cb => cb != null && cb.pqsController != null));
            RegisterMethod(new Method<CelestialBody, bool>("IsHomeWorld", cb => cb != null && cb.isHomeWorld));
            RegisterMethod(new Method<CelestialBody, bool>("IsPlanet", cb =>
                cb != null && (cb.referenceBody != null && cb != FlightGlobals.Bodies[0] && cb.referenceBody == FlightGlobals.Bodies[0])));
            RegisterMethod(new Method<CelestialBody, bool>("IsMoon", cb =>
                cb != null && (cb.referenceBody != null && cb != FlightGlobals.Bodies[0] && cb.referenceBody != FlightGlobals.Bodies[0])));
            RegisterMethod(new Method<CelestialBody, bool>("IsOrbitalSurveyComplete", cb => cb != null && ResourceScenario.Instance != null &&
                ResourceScenario.Instance.gameSettings.GetPlanetScanInfo().Where(psd => psd.PlanetId == cb.flightGlobalsIndex).Any(), false));

            RegisterMethod(new Method<CelestialBody, double>("Radius", cb => cb != null ? cb.Radius : 0.0));
            RegisterMethod(new Method<CelestialBody, double>("AtmosphereAltitude", cb => cb != null ? cb.atmosphereDepth : 0.0));
            RegisterMethod(new Method<CelestialBody, double>("SphereOfInfluence", cb => cb != null ? cb.sphereOfInfluence : 0.0));
            
            RegisterMethod(new Method<CelestialBody, CelestialBody>("Parent", cb => cb != null ? cb.referenceBody : null));
            RegisterMethod(new Method<CelestialBody, List<CelestialBody>>("Children", cb => cb != null ? cb.orbitingBodies : new List<CelestialBody>()));

            RegisterMethod(new Method<CelestialBody, List<Biome>>("Biomes", cb => cb != null && cb.BiomeMap != null ?
                cb.BiomeMap.Attributes.Select(y => new Biome(cb, y.name)).ToList() : new List<Biome>()));

            RegisterGlobalFunction(new Function<CelestialBody>("HomeWorld", () => FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).First()));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("AllBodies", () => FlightGlobals.Bodies));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("OrbitedBodies", () => ProgressTracking.Instance == null ?
                new List<CelestialBody>() :
                ProgressTracking.Instance.celestialBodyNodes.Where(subtree => subtree.orbit.IsReached).
                Select<CelestialBodySubtree, CelestialBody>(subtree => subtree.Body).ToList(), false));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("LandedBodies", () => ProgressTracking.Instance == null ?
                new List<CelestialBody>() :
                ProgressTracking.Instance.celestialBodyNodes.Where(subtree => subtree.landing.IsReached).
                Select<CelestialBodySubtree, CelestialBody>(subtree => subtree.Body).ToList(), false));
            RegisterGlobalFunction(new Function<CelestialBody, CelestialBody>("CelestialBody", cb => cb));
        }

        public CelestialBodyParser()
        {
        }

        internal override U ConvertType<U>(CelestialBody value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.theName;
            }
            return base.ConvertType<U>(value);
        }

        internal override CelestialBody ParseIdentifier(Token token)
        {
            if (token.sval.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }
            return ConfigNodeUtil.ParseCelestialBodyValue(token.sval);
        }
    }
}
