using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for strings
    /// </summary>
    public class StringExpressionParser : ClassExpressionParser<string>, IExpressionParserRegistrer
    {
        public void RegisterExpressionParsers()
        {
            ExpressionParserUtil.RegisterParserType(typeof(string), typeof(StringExpressionParser));
        }

        public StringExpressionParser()
        {
        }

        /// <summary>
        /// String statements work differently.  Basically it's just a search and replace for
        /// @identifier nodes, with the rest treated as a string literal.
        /// </summary>
        /// <returns>The full string after parsing</returns>
        protected override string ParseStatement()
        {
            string value = "";
            while (expression.Length > 0)
            {
                int index = expression.IndexOf("@");
                if (index >= 0)
                {
                    value += expression.Substring(0, index);
                    expression = expression.Substring(index);
                    value += ParseSpecialIdentifier(ParseSpecialIdentifier());
                }
                else
                {
                    value += expression;
                    expression = "";
                }
            }

            value = value.Replace("\\n", "\n");

            return value;
        }
    }
}
