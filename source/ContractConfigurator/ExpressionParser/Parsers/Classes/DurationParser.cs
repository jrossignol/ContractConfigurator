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
    public class DurationParser : ClassExpressionParser<Duration>, IExpressionParserRegistrer
    {
        private static System.Random random = new System.Random();

        static DurationParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Duration), typeof(DurationParser));
        }

        internal static void RegisterMethods()
        {
            RegisterLocalFunction(new Function<Duration, Duration, Duration>("Random", RandomMinMax, false));
            RegisterLocalFunction(new Function<Duration, Duration, Duration>("Round", Round));
        }

        public DurationParser()
        {
        }

        private static Duration RandomMinMax(Duration min, Duration max)
        {
            double val = random.NextDouble() * (max.Value - min.Value) + min.Value;
            return new Duration(val);
        }

        private static Duration Round(Duration value, Duration precision)
        {
            double val = precision.Value * Math.Round(value.Value / precision.Value);
            return new Duration(val);
        }

        internal override U ConvertType<U>(Duration value)
        {
            if (typeof(U) == typeof(double))
            {
                return (U)(object)value.Value;
            }
            return base.ConvertType<U>(value);
        }

        internal override bool ConvertableFrom(Type type)
        {
            return type == typeof(int) || type == typeof(double);
        }

        internal override Duration ConvertFrom<U>(U value)
        {
            double dval = (double)(object)value;
            return new Duration(dval);
        }

        internal override Token ParseNumericConstant()
        {
            // Try to parse more, as durations can have spaces
            Match m = Regex.Match(expression, @"^((?>\s*[\d][\w\d]*[\w])+).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");

            return new ValueToken<Duration>(new Duration(identifier));
        }
    }
}
