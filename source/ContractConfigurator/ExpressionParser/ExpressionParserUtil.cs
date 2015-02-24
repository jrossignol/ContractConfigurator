using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Types of expresion tokens.
    /// </summary>
    public enum TokenType
    {
        IDENTIFIER,
        SPECIAL_IDENTIFIER,
        VALUE,
        OPERATOR,
        START_BRACKET,
        END_BRACKET
    }

    /// <summary>
    /// A parsed token.
    /// </summary>
    public class Token
    {
        public TokenType tokenType;
        public string sval;

        public Token(TokenType type)
        {
            tokenType = type;

            if (tokenType == TokenType.START_BRACKET)
            {
                sval = "(";
            }
            else if (tokenType == TokenType.END_BRACKET)
            {
                sval = ")";
            }
        }

        public Token(TokenType type, string s)
        {
            tokenType = type;
            sval = s;
        }
    }

    /// <summary>
    /// A token with a value type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValueToken<T> : Token
    {
        public T val;

        public ValueToken(T t)
            : base(TokenType.VALUE)
        {
            val = t;
            sval = t.ToString();
        }
    }

    /// <summary>
    /// Utility class for holding non-type specific expression parser details.
    /// </summary>
    public static class ExpressionParserUtil
    {
        public static char[] WHITESPACE_OR_OPERATOR =
        {
            ' ', '\t', '\n', '|', '&', '+', '-', '!', '<', '>', '=', '*', '/', ')'
        };

        // List of tokens and their precedence
        private static string[][] PRECENDENCE_CONSTS =
        {
            new string[] { "||" },
            new string[] { "&&" },
            new string[] { "!", "<", ">", "!=", "==", "<=", ">=" },
            new string[] { "-", "+" },
            new string[] { "*", "/" }
        };
        public static Dictionary<string, int> precedence = new Dictionary<string, int>();

        private static Dictionary<Type, Type> parserTypes = new Dictionary<Type, Type>();

        static ExpressionParserUtil()
        {
            // Create the precendence map
            if (precedence.Count == 0)
            {
                for (int i = 0; i < PRECENDENCE_CONSTS.Length; i++)
                {
                    foreach (string token in PRECENDENCE_CONSTS[i])
                    {
                        precedence[token] = i;
                    }
                }
            }

            // Register each type of expression parser
            Debug.Log("Registring dudes");
            foreach (Type subclass in ContractConfigurator.GetAllTypes<IExpressionParserRegistrer>())
            {
                if (subclass.IsClass && !subclass.IsAbstract)
                {
                    Debug.Log("Registring dude: " + subclass);
                    IExpressionParserRegistrer r = Activator.CreateInstance(subclass) as IExpressionParserRegistrer;
                    var method = subclass.GetMethod("RegisterExpressionParsers");
                    method.Invoke(r, new object[] { });
                }
            }
        }

        /// <summary>
        /// Registers a parser for the given type of object.
        /// </summary>
        /// <param name="objectType">Type of object that the given parser will handle expressions for.</param>
        /// <param name="parserType">Type of the parser.</param>
        public static void RegisterParserType(Type objectType, Type parserType)
        {
            parserTypes[objectType] = parserType;
        }

        /// <summary>
        /// Gets an ExpressionParser for the given type.
        /// </summary>
        /// <typeparam name="T">The type to get a parser for</typeparam>
        /// <returns>An instance of the expression parser, or null if none can be created</returns>
        public static ExpressionParser<T> GetParser<T>()
        {
            if (parserTypes.ContainsKey(typeof(T)))
            {
                return (ExpressionParser<T>)Activator.CreateInstance(parserTypes[typeof(T)]);
            }

            return null;
        }
    }
}
