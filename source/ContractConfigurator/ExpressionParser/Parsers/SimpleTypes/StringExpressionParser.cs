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
                expression = expression.Trim();
                string savedExpression = expression;
                Token token = null;
                try
                {
                    token = ParseToken();
                }
                catch { }
                finally
                {
                    expression = savedExpression;
                }

                if (token != null)
                    LoggingUtil.LogInfo(this, "The first char (" + expression + ") is: " + token.sval.First());

                bool quoted = token != null && token.tokenType == TokenType.QUOTE;
                if (quoted)
                {
                    expression = expression.Substring(1);
                }
                else if (token != null && token.tokenType == TokenType.SPECIAL_IDENTIFIER)
                {
                    return base.ParseStatement<TResult>();
                }
                else
                {
                    // Check for an immediate function call 
                    Match m = Regex.Match(expression, @"^\w[\w\d]*\(");
                    if (m.Success)
                    {
                        return base.ParseStatement<TResult>();
                    }
                }

                string value = "";
                while (expression.Length > 0)
                {
                    // Look for special identifiers
                    int specialIdentifierIndex = expression.IndexOf("@");

                    // Look for function calls
                    Match m = Regex.Match(expression, @"\s\w[\w\d]*\(");
                    int functionIndex = m.Index;

                    // Look for an end quote
                    int quoteIndex = quoted ? expression.IndexOf('"') : -1;
                    if (quoteIndex > 0)
                    {
                        if (expression.Substring(quoteIndex-1, 1) == "\\")
                        {
                            quoteIndex = -1;
                        }
                    }

                    if (m.Success && (specialIdentifierIndex == -1 || functionIndex < specialIdentifierIndex) && (quoteIndex == -1 || functionIndex < quoteIndex))
                    {
                        specialIdentifierIndex = -1;
                        quoteIndex = -1;
                    }
                    else if (specialIdentifierIndex == -1 || quoteIndex < specialIdentifierIndex)
                    {
                        specialIdentifierIndex = -1;
                        functionIndex = -1;
                    }
                    else
                    {
                        functionIndex = -1;
                        quoteIndex = -1;
                    }

                    if (functionIndex >= 0)
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
                    else if (quoteIndex >= 0)
                    {
                        value += expression.Substring(0, quoteIndex);
                        expression = expression.Substring(quoteIndex+1);
                        break;
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
