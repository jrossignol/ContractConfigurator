using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator.ExpressionParser
{
    public class EnumExpressionParser<T> : ValueExpressionParser<T> where T : struct, IConvertible
    {
        static System.Random random = new System.Random();

        static EnumExpressionParser()
        {
            RegisterMethods();
        }

        internal static void RegisterMethods()
        {
            RegisterLocalFunction(new Function<T>("Random", RandomEnumValue, false));
            RegisterLocalFunction(new Function<List<T>>("All", () => Enum.GetValues(typeof(T)).OfType<T>().ToList()));
        }

        protected static T RandomEnumValue()
        {
            Array values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(random.Next(values.Length));
        }

        public EnumExpressionParser()
            : base()
        {
        }

        internal override T ParseIdentifier(Token token)
        {
            return (T)Enum.Parse(typeof(T), token.sval);
        }

        internal override bool EQ(T a, T b)
        {
            return a.ToInt32(null) == b.ToInt32(null);
        }

        internal override bool NE(T a, T b)
        {
            return a.ToInt32(null) == b.ToInt32(null);
        }
    }
}
