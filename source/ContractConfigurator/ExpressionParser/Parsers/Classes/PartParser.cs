using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Part (AvailablePart).
    /// </summary>
    public class PartParser : ClassExpressionParser<AvailablePart>, IExpressionParserRegistrer
    {
        static PartParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(AvailablePart), typeof(PartParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<AvailablePart, PartCategories>("Category", p => p == null ? 0 : p.category));
            RegisterMethod(new Method<AvailablePart, float>("Cost", p => p == null ? 0.0f : p.cost));
            RegisterMethod(new Method<AvailablePart, string>("Description", p => p == null ? "" : p.description));
            RegisterMethod(new Method<AvailablePart, string>("Manufacturer", p => p == null ? "" : p.manufacturer));
            RegisterMethod(new Method<AvailablePart, float>("Size", p => p == null ? 0.0f : p.partSize));
            RegisterMethod(new Method<AvailablePart, string>("TechRequired", p => p == null ? "" : p.TechRequired));

            RegisterGlobalFunction(new Function<List<AvailablePart>>("AllParts", () => PartLoader.Instance.parts));
            RegisterGlobalFunction(new Function<AvailablePart, AvailablePart>("AvailablePart", p => p));
        }

        public PartParser()
        {
        }

        internal override U ConvertType<U>(AvailablePart value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.name);
            }
            return base.ConvertType<U>(value);
        }

        internal override AvailablePart ParseIdentifier(Token token)
        {
            // Try to parse more, as part names can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[A-Za-z][\w\d]*)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            // Underscores in part names get replaced with spaces.  Nobody knows why.
            string partName = identifier.Replace('_', '.');

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            // Get the part
            AvailablePart part = PartLoader.getPartInfoByName(partName);
            if (part == null)
            {
                throw new ArgumentException("'" + identifier + "' is not a valid Part.");
            }

            currentDataNode.SetDeterministic(currentKey, false);

            return part;
        }
    }
}
