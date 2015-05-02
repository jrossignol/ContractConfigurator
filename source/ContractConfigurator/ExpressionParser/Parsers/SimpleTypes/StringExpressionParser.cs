using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for strings
    /// </summary>
    public class StringExpressionParser : ClassExpressionParser<string>, IExpressionParserRegistrer
    {
        static StringExpressionParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(string), typeof(StringExpressionParser));
        }

        public StringExpressionParser()
        {
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<string, string>("ToLower", s => s == null ? null : s.ToLower()));
            RegisterMethod(new Method<string, string>("ToUpper", s => s == null ? null : s.ToUpper()));
        }

        /// <summary>
        /// String statements work differently.  Basically it's just a search and replace for
        /// @identifier nodes, with the rest treated as a string literal.
        /// </summary>
        /// <returns>The full string after parsing</returns>
        internal override TResult ParseStatement<TResult>()
        {
            verbose &= LogEntryDebug<TResult>("ParseStatement");
            try
            {
                string value = "";
                while (expression.Length > 0)
                {
                    int specialIdentifierIndex = expression.IndexOf("@");
                    Match m = Regex.Match(expression, @"\s\w[\w\d]*\(");
                    int functionIndex = m.Index;

                    if (m.Success && specialIdentifierIndex >= 0)
                    {
                        if (functionIndex < specialIdentifierIndex)
                        {
                            specialIdentifierIndex = -1;
                        }
                        else
                        {
                            functionIndex = -1;
                        }
                    }

                    if (m.Success)
                    {
                        value += expression.Substring(0, functionIndex+1);
                        expression = expression.Substring(functionIndex);
                        Token t = ParseToken();
                        LoggingUtil.LogDebug(this, "    " + t.sval);
                        value += ParseMethod<string>(t, null, true);
                    }
                    else if (specialIdentifierIndex >= 0)
                    {
                        value += expression.Substring(0, specialIdentifierIndex);
                        expression = expression.Substring(specialIdentifierIndex);
                        value += ParseSpecialIdentifier(ParseSpecialIdentifier());
                    }
                    else
                    {
                        value += expression;
                        expression = "";
                    }
                }

                value = value.Replace("\\n", "\n");

                verbose &= LogExitDebug<TResult>("ParseStatement", value);
                return (TResult)(object)value;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseStatement");
                throw;
            }
        }
    }
}
