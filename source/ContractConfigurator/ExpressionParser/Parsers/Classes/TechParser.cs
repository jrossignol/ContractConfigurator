using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Tech.
    /// </summary>
    public class TechParser : ClassExpressionParser<Tech>, IExpressionParserRegistrer
    {
        static TechParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Tech), typeof(TechParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<Tech, bool>("IsUnlocked", t => t == null ? false : t.IsUnlocked(), false));
            RegisterMethod(new Method<Tech, bool>("IsReadyToUnlock", t => t == null ? false : t.IsReadyToUnlock(), false));
            RegisterMethod(new Method<Tech, float>("Cost", t => t == null ? 0.0f : t.cost));
            RegisterMethod(new Method<Tech, string>("Description", t => t == null ? "" : t.description));
            RegisterMethod(new Method<Tech, int>("Level", t => t == null ? 0 : t.level));
            RegisterMethod(new Method<Tech, List<Tech>>("Parents", t => t == null ? new List<Tech>() : t.ParentNodes().ToList()));
            RegisterMethod(new Method<Tech, List<Tech>>("Children", t => t == null ? new List<Tech>() : t.children.ToList()));

            RegisterGlobalFunction(new Function<List<Tech>>("AllTech", () => Tech.AllTech().ToList(), false));
            RegisterGlobalFunction(new Function<List<Tech>>("UnlockedTech", () => Tech.AllTech().Where(t => t.IsUnlocked()).ToList(), false));
            RegisterGlobalFunction(new Function<Tech, Tech>("Tech", t => t));

            RegisterGlobalFunction(new Function<int>("MaxTechLevelUnlocked", MaxTechLevelUnlocked));
        }

        public TechParser()
        {
        }

        private static int MaxTechLevelUnlocked()
        {
            return Tech.AllTech().Where(t => t.IsUnlocked()).Select(t => t.level).Max();
        }

        internal override U ConvertType<U>(Tech value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.title);
            }
            return base.ConvertType<U>(value);
        }

        internal override Tech ParseIdentifier(Token token)
        {
            // Try to parse more, as Tech names can have spaces (wait, can they?)
            Match m = Regex.Match(expression, @"^((?>\s*[A-Za-z][\w\d]*)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            currentDataNode.SetDeterministic(currentKey, false);

            return Tech.GetTech(identifier);
        }
    }
}
