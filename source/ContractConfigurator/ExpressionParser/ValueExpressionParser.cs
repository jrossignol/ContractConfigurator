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

        public void ExecuteAndStoreExpression(string key, string expression)
        {
            PersistentDataStore.Instance.Store<T>(key, ExecuteExpression(expression));
        }

        /// <summary>
        /// Parses an identifier for a value stored in the persistant data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the identifier</returns>
        protected override T ParseIdentifier(Token token)
        {
            if (typeof(T) == typeof(double))
            {
                return parseMode ? default(T) : PersistentDataStore.Instance.Retrieve<T>(token.sval);
            }
            else
            {
                return base.ParseIdentifier(token);
            }

        }
    }

    public class ComparableValueExpressionParser<T> : ValueExpressionParser<T> where T : struct, IComparable<T>
    {
        public ComparableValueExpressionParser()
            : base()
        {
        }

        protected override bool LT(T a, T b)
        {
            return a.CompareTo(b) < 0;
        }

        protected override bool LE(T a, T b)
        {
            return a.CompareTo(b) <= 0;
        }

        protected override bool EQ(T a, T b)
        {
            return a.CompareTo(b) == 0;
        }

        protected override bool NE(T a, T b)
        {
            return a.CompareTo(b) != 0;
        }

        protected override bool GE(T a, T b)
        {
            return a.CompareTo(b) >= 0;
        }

        protected override bool GT(T a, T b)
        {
            return a.CompareTo(b) > 0;
        }
    }
}
