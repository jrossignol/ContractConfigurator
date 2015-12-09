using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ContractConfigurator.Util;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Biome.
    /// </summary>
    public class BiomeParser : ClassExpressionParser<Biome>, IExpressionParserRegistrer
    {
        static BiomeParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Biome), typeof(BiomeParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<Biome, string>("Name", biome => biome == null ? "" : Biome.PrintBiomeName(biome.biome)));
            RegisterMethod(new Method<Biome, string>("FullName", biome => biome == null ? "" : biome.ToString()));
            RegisterMethod(new Method<Biome, CelestialBody>("CelestialBody", biome => biome == null ? null : biome.body));
            RegisterMethod(new Method<Biome, bool>("IsKSC", biome => biome == null ? false : biome.IsKSC()));
            RegisterMethod(new Method<Biome, float>("RemainingScience", RemainingScience));
            RegisterMethod(new Method<Biome, Vessel.Situations>("PrimarySituation", GetPrimarySituation));

            RegisterMethod(new Method<Biome, List<Location>>("DifficultLocations", biome => biome == null ?
                new List<Location>() : BiomeTracker.GetDifficultLocations(biome.body, biome.biome).Select(v => new Location(biome.body, v.y, v.x)).ToList()));

            RegisterGlobalFunction(new Function<List<Biome>>("KSCBiomes", () => Biome.KSCBiomes.Select(b =>
                new Biome(FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).Single(), b)).ToList(), false));
            RegisterGlobalFunction(new Function<List<Biome>>("MainKSCBiomes", () => Biome.MainKSCBiomes.Select(b =>
                new Biome(FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).Single(), b)).ToList(), false));
        }

        public BiomeParser()
        {
        }

        private static float RemainingScience(Biome biome)
        {
            if (biome == null || HighLogic.CurrentGame == null)
            {
                return 0.0f;
            }

            return Science.GetSubjects(new CelestialBody[] { biome.body }, null, b => b == biome.biome).Sum(subj =>
                subj.scienceCap * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier - subj.science);
        }

        private static Vessel.Situations GetPrimarySituation(Biome biome)
        {
            if (biome == null)
            {
                return Vessel.Situations.LANDED;
            }

            return BiomeTracker.GetPrimarySituation(biome.body, biome.biome);
        }

        public override Biome ParseIdentifier(Token token)
        {
            // Try to parse more, as biome names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[\w\d]+)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            // Special case for null
            if (identifier == "null")
            {
                return null;
            }

            return new Biome(null, identifier);
        }
    }
}
