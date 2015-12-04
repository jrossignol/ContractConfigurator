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
    public class ClassExpressionParser<T> : ExpressionParser<T> where T : class
    {
        public ClassExpressionParser()
            : base()
        {
        }

        static ClassExpressionParser()
        {
            RegisterClassMethods();
        }

        public static void RegisterClassMethods()
        {
            RegisterMethod(new Method<T, string>("ToString", v => v == null ? "" : GetParser<T>().ConvertType<string>(v)));
        }

        public override bool EQ(T a, T b)
        {
            if (a == b)
            {
                return true;
            }
            if (a == null || b == null)
            {
                return false;
            }

            return a.Equals(b);
        }

        public override bool NE(T a, T b)
        {
            return !EQ(a, b);
        }
    }

    public class ComparableClassExpressionParser<T> : ClassExpressionParser<T> where T : class, IComparable<T>
    {
        public ComparableClassExpressionParser()
            : base()
        {
        }

        public override bool LT(T a, T b)
        {
            return a.CompareTo(b) < 0;
        }

        public override bool LE(T a, T b)
        {
            return a.CompareTo(b) <= 0;
        }

        public override bool EQ(T a, T b)
        {
            return a.CompareTo(b) == 0;
        }

        public override bool NE(T a, T b)
        {
            return a.CompareTo(b) != 0;
        }

        public override bool GE(T a, T b)
        {
            return a.CompareTo(b) >= 0;
        }

        public override bool GT(T a, T b)
        {
            return a.CompareTo(b) > 0;
        }

        public override T ParseIdentifier(Token token)
        {
            if (token.sval.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }
            return base.ParseIdentifier(token);
        }
    }
}
