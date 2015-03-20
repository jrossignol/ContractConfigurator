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
    public class ValueExpressionParser<T> : ExpressionParser<T> where T : struct
    {
        public ValueExpressionParser()
            : base()
        {
        }
    }

    public class ComparableValueExpressionParser<T> : ValueExpressionParser<T> where T : struct, IComparable<T>
    {
        public ComparableValueExpressionParser()
            : base()
        {
        }

        internal override bool LT(T a, T b)
        {
            return a.CompareTo(b) < 0;
        }

        internal override bool LE(T a, T b)
        {
            return a.CompareTo(b) <= 0;
        }

        internal override bool EQ(T a, T b)
        {
            return a.CompareTo(b) == 0;
        }

        internal override bool NE(T a, T b)
        {
            return a.CompareTo(b) != 0;
        }

        internal override bool GE(T a, T b)
        {
            return a.CompareTo(b) >= 0;
        }

        internal override bool GT(T a, T b)
        {
            return a.CompareTo(b) > 0;
        }
    }
}
