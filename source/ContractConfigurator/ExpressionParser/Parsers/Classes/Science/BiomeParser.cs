using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

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

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<Biome, CelestialBody>("CelestialBody", biome => biome == null ? null : biome.body));
            RegisterMethod(new Method<Biome, bool>("IsKSC", biome => biome == null ? false : biome.IsKSC()));
        }

        public BiomeParser()
        {
        }

        internal override U ConvertType<U>(Biome value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.biome;
            }
            return base.ConvertType<U>(value);
        }

        internal override Biome ParseIdentifier(Token token)
        {
            // Try to parse more, as biome names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[A-Za-z][\w\d]*)+).*");
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
