using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for strings
    /// </summary>
    public class StringExpressionParser : ClassExpressionParser<string>, IExpressionParserRegistrer
    {
        public override MethodInfo methodParseStatement { get { return _methodParseStatement; } }
        static MethodInfo _methodParseStatement = typeof(StringExpressionParser).GetMethods(BindingFlags.Public | BindingFlags.Instance).
            Where(m => m.Name == "ParseStatement" && m.GetParameters().Count() == 0).Single();
        public override MethodInfo methodParseMethod { get { return _methodParseMethod; } }
        static MethodInfo _methodParseMethod = typeof(StringExpressionParser).GetMethods(BindingFlags.Public | BindingFlags.Instance).
            Where(m => m.Name == "ParseMethod" && m.GetParameters().Count() == 3).Single();

        static System.Random random = new System.Random();

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

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<string, string>("ToLower", s => s == null ? "" : s.ToLower()));
            RegisterMethod(new Method<string, string>("ToUpper", s => s == null ? "" : s.ToUpper()));
            RegisterMethod(new Method<string, string>("FirstCap", s => s == null ? "" : s.Count() > 2 ? s.Substring(0, 1).ToUpper() + s.Substring(1) : s.ToUpper()));
            RegisterMethod(new Method<string, string>("FirstWord", s => s == null ? "" : s.Split(new char[] { ' ' }).FirstOrDefault()));
            RegisterMethod(new Method<string, string, bool>("Contains", (s, value) => s.Contains(value)));
            RegisterMethod(new Method<string, string, bool>("StartsWith", (s, value) => s.StartsWith(value)));
            RegisterMethod(new Method<string, string, bool>("EndsWith", (s, value) => s.EndsWith(value)));
        }

        /// <summary>
        /// String statements work differently.  Basically it's just a search and replace for
        /// @identifier nodes, with the rest treated as a string literal.
        /// </summary>
        /// <returns>The full string after parsing</returns>
        public override TResult ParseStatement<TResult>()
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

                string value = "";

                bool quoted = token != null && token.tokenType == TokenType.QUOTE;
                if (quoted)
                {
                    expression = expression.Substring(1);
                }
                else if (token != null && token.tokenType == TokenType.SPECIAL_IDENTIFIER)
                {
                    TResult result = base.ParseStatement<TResult>();
                    if (parentParser != null)
                    {
                        return result;
                    }

                    value = (string)(object)result ?? "";
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

                while (expression.Length > 0)
                {
                    // Look for special identifiers
                    int specialIdentifierIndex = expression.IndexOf("@");

                    // Look for data store identifiers
                    Match m = Regex.Match(expression, @"\$[A-Za-z]");
                    int dataStoreIdentifierIndex = m.Success ? m.Index : -1;

                    // Look for function calls
                    m = Regex.Match(expression, @"(\A|\s)\w[\w\d]*\(");
                    int functionIndex = m == Match.Empty ? -1 : m.Index;

                    // Look for an end quote
                    int quoteIndex = quoted ? expression.IndexOf('"') : -1;
                    if (quoteIndex > 0)
                    {
                        if (expression.Substring(quoteIndex-1, 1) == "\\")
                        {
                            quoteIndex = -1;
                        }
                    }

                    if (m.Success && (specialIdentifierIndex == -1 || functionIndex < specialIdentifierIndex) &&
                        (dataStoreIdentifierIndex == -1 || functionIndex < dataStoreIdentifierIndex) && (quoteIndex == -1 || functionIndex < quoteIndex))
                    {
                        specialIdentifierIndex = -1;
                        dataStoreIdentifierIndex = -1;
                        quoteIndex = -1;
                    }
                    else if (quoteIndex != -1 && (specialIdentifierIndex == -1 || quoteIndex < specialIdentifierIndex) &&
                        (dataStoreIdentifierIndex == -1 || quoteIndex < dataStoreIdentifierIndex))
                    {
                        specialIdentifierIndex = -1;
                        dataStoreIdentifierIndex = -1;
                        functionIndex = -1;
                    }
                    else if (dataStoreIdentifierIndex != -1 && (specialIdentifierIndex == -1 || dataStoreIdentifierIndex < specialIdentifierIndex))
                    {
                        specialIdentifierIndex = -1;
                        functionIndex = -1;
                        quoteIndex = -1;
                    }
                    else
                    {
                        dataStoreIdentifierIndex = -1;
                        functionIndex = -1;
                        quoteIndex = -1;
                    }

                    if (functionIndex >= 0)
                    {
                        if (functionIndex > 0 || expression[0] == ' ')
                        {
                            value += expression.Substring(0, functionIndex + 1);
                        }
                        expression = expression.Substring(functionIndex);
                        Token t = ParseToken();

                        bool canParse = false;
                        try
                        {
                            Function selectedMethod = null;
                            GetCalledFunction(t.sval, ref selectedMethod, true);
                            canParse = true;
                        }
                        catch { }

                        if (canParse)
                        {
                            value += ParseMethod<string>(t, null, true);
                        }
                        else
                        {
                            value += t.sval;
                        }
                    }
                    else if (specialIdentifierIndex >= 0)
                    {
                        value += expression.Substring(0, specialIdentifierIndex);
                        expression = expression.Substring(specialIdentifierIndex);
                        value += ParseSpecialIdentifier(ParseSpecialIdentifier());
                    }
                    else if (dataStoreIdentifierIndex >= 0)
                    {
                        value += expression.Substring(0, dataStoreIdentifierIndex);
                        expression = expression.Substring(dataStoreIdentifierIndex);
                        
                        // Workaround for limitation/bug in how the expression factory works.  Will probably need to revisit
                        if (currentDataNode.Factory is Behaviour.ExpressionFactory)
                        {
                            value += expression.Substring(0, 1);
                            expression = expression.Substring(1);
                        }
                        else
                        {
                            value += ParseDataStoreIdentifier(ParseDataStoreIdentifier());
                        }
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

                if (expression.Length > 0 && parentParser == null)
                {
                    value = ParseStatement<string>(value);
                }

                verbose &= LogExitDebug<TResult>("ParseStatement", value);
                return (TResult)(object)value;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseStatement");
                throw;
            }
        }

        public override string Add(string a, string b)
        {
            return string.Concat(a, b);
        }
    }
}
