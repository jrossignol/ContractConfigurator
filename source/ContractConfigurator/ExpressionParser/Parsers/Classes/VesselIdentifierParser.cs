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
            Match m = Regex.Match(expression, @"^((?>\s*[A-Za-z][\w\d-]*)+).*");
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
