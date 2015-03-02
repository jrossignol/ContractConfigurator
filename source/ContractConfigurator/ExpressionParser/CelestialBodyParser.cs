using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            RegisterMethod(new Method<CelestialBody, bool>("HasAtmosphere", cb => cb.atmosphere));
            RegisterMethod(new Method<CelestialBody, bool>("HasOcean", cb => cb.ocean));
            RegisterMethod(new Method<CelestialBody, bool>("HasSurface", cb => cb.pqsController != null));
            RegisterMethod(new Method<CelestialBody, bool>("IsHomeWorld", cb => cb.isHomeWorld));
            RegisterMethod(new Method<CelestialBody, bool>("IsPlanet", cb => cb.referenceBody != null && cb.referenceBody.referenceBody == null));
            RegisterMethod(new Method<CelestialBody, bool>("IsMoon", cb => cb.referenceBody != null && cb.referenceBody.referenceBody != null));

            RegisterMethod(new Method<CelestialBody, double>("Radius", cb => cb.Radius));
            RegisterMethod(new Method<CelestialBody, float>("AtmosphereAltitude", cb => cb.maxAtmosphereAltitude));
            RegisterMethod(new Method<CelestialBody, double>("SphereOfInfluence", cb => cb.sphereOfInfluence));
            
            RegisterMethod(new Method<CelestialBody, CelestialBody>("Parent", cb => cb.referenceBody));
            RegisterMethod(new Method<CelestialBody, List<CelestialBody>>("Children", cb => cb.orbitingBodies));

            RegisterGlobalFunction(new Function<CelestialBody>("HomeWorld", () => FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).First()));
            RegisterGlobalFunction(new Function<List<CelestialBody>>("AllBodies", () => FlightGlobals.Bodies));
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
            return ConfigNodeUtil.ParseCelestialBodyValue(token.sval);
        }
    }
}
