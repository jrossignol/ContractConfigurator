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

        public static void RegisterMethods()
        {
            RegisterGlobalFunction(new Function<VesselIdentifier, VesselIdentifier>("VesselIdentifier", v => v));
        }

        public VesselIdentifierParser()
        {
        }

        public override U ConvertType<U>(VesselIdentifier value)
        {
            if (typeof(U) == typeof(Vessel))
            {
                if (!parseMode && value != null)
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
        
        public override bool ConvertableFrom(Type type)
        {
            return type == typeof(string);
        }

        public override VesselIdentifier ConvertFrom<U>(U value)
        {
            string identifier = (string)(object)value;
            return string.IsNullOrEmpty(identifier) ? null : new VesselIdentifier(identifier);
        }

        public override VesselIdentifier ParseIdentifier(Token token)
        {
            // Vessel identifiers are greedy - grab the rest of the expression
            string identifier = token.sval + expression;
            expression = "";

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            return new VesselIdentifier(identifier);
        }
    }
}
