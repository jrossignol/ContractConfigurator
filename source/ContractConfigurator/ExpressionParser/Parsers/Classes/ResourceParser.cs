using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Kerbal (ProtoCrewMember).
    /// </summary>
    public class ResourceParser : ClassExpressionParser<Resource>, IExpressionParserRegistrer
    {
        static ResourceParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Resource), typeof(ResourceParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<Resource, double>("Density", r => r != null ? r.res.density : 1.0));

            RegisterGlobalFunction(new Function<Resource, Resource>("Resource", r => r));
        }

        public ResourceParser()
        {
        }

        public override bool ConvertableFrom(Type type)
        {
            return type == typeof(string);
        }

        public override Resource ConvertFrom<U>(U value)
        {
            if (typeof(U) == typeof(string))
            {
                string sVal = (string)(object)value;

                // Get the actual resource
                var enumerator = PartResourceLibrary.Instance.resourceDefinitions.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.name == sVal)
                        {
                            return new Resource(enumerator.Current);
                        }
                    }
                }
                finally
                {
                    enumerator.Dispose();
                }
                throw new ArgumentException("'" + sVal + "' is not a valid resource.");
            }
            throw new DataStoreCastException(typeof(U), typeof(Resource));
        }

        public override U ConvertType<U>(Resource value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)(value == null ? "" : value.ToString());
            }
            return base.ConvertType<U>(value);
        }

        public override Resource ParseIdentifier(Token token)
        {
            // Try to parse more, as resource names can have spaces and other charactes
            Match m = Regex.Match(expression, @"^((?>\s*[\w\d-_]+)+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");
            identifier = token.sval + identifier;

            if (identifier.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            // Get the actual resourece
            var enumerator = PartResourceLibrary.Instance.resourceDefinitions.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.name == identifier)
                    {
                        return new Resource(enumerator.Current);
                    }
                }
            }
            finally
            {
                enumerator.Dispose();
            }
            throw new ArgumentException("'" + identifier + "' is not a valid resource.");
        }
    }
}
