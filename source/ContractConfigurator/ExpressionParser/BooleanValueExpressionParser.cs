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

        public BooleanValueExpressionParser()
            : base()
        {
        }

        protected override bool EQ(bool a, bool b)
        {
            return a == b;
        }

        protected override bool NE(bool a, bool b)
        {
            return a != b;
        }

        protected override bool Not(bool val)
        {
            return !val;
        }

        protected override bool Or(bool a, bool b)
        {
            return a || b;
        }

        protected override bool And(bool a, bool b)
        {
            return a && b;
        }

        /// <summary>
        /// Parses an identifier for a value stored in the persistant data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the identifier</returns>
        protected override bool ParseIdentifier(Token token)
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
                throw new NotSupportedException("Invalid boolean constant '" + token.sval + "'.");
            }
        }
    }
}
