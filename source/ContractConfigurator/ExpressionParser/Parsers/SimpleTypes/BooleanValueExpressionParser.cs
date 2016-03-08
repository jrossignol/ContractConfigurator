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
    public class BooleanValueExpressionParser : ValueExpressionParser<bool>, IExpressionParserRegistrer
    {
        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(bool), typeof(BooleanValueExpressionParser));
        }

        static BooleanValueExpressionParser()
        {
            RegisterMethods();
        }

        public static void RegisterMethods()
        {
            RegisterGlobalFunction(new Function<bool, bool>("bool", t => t));
        }

        public BooleanValueExpressionParser()
            : base()
        {
        }

        public override bool EQ(bool a, bool b)
        {
            return a == b;
        }

        public override bool NE(bool a, bool b)
        {
            return a != b;
        }

        public override bool Not(bool val)
        {
            return !val;
        }

        public override bool Or(bool a, bool b)
        {
            return a || b;
        }

        public override bool And(bool a, bool b)
        {
            return a && b;
        }

        /// <summary>
        /// Parses an identifier for a value stored in the persistant data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the identifier</returns>
        public override bool ParseIdentifier(Token token)
        {
            if (string.Compare(token.sval, "true", true) == 0)
            {
                return true;
            }
            else if (string.Compare(token.sval, "false", true) == 0)
            {
                return false;
            }
            else
            {
                expression = token.sval + expression;
                throw new WrongDataType(typeof(double), typeof(bool));
            }
        }
    }
}
