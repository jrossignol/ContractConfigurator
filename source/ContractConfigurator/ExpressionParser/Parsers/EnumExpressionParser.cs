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
