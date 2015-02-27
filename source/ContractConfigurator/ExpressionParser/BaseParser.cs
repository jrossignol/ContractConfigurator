using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Common base class (without typing) for all expression parsers.
    /// </summary>
    public abstract class BaseParser
    {
        /// <summary>
        /// Types of expresion tokens.
        /// </summary>
        protected enum TokenType
        {
            IDENTIFIER,
            SPECIAL_IDENTIFIER,
            VALUE,
            OPERATOR,
            START_BRACKET,
            END_BRACKET,
            COMMA,
            FUNCTION,
            METHOD
        }

        /// <summary>
        /// A parsed token.
        /// </summary>
        protected class Token
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
                else if (tokenType == TokenType.COMMA)
                {
                    sval = ",";
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
        protected class ValueToken<T> : Token
        {
            public T val;

            public ValueToken(T t)
                : base(TokenType.VALUE)
            {
                val = t;
                sval = t.ToString();
            }
        }

        public static char[] WHITESPACE_OR_OPERATOR =
        {
            ' ', '\t', '\n', '|', '&', '+', '-', '!', '<', '>', '=', '*', '/', '(', ')', ',', '?', ':'
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

        static BaseParser()
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
            foreach (Type subclass in ContractConfigurator.GetAllTypes<IExpressionParserRegistrer>())
            {
                if (subclass.IsClass && !subclass.IsAbstract)
                {
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

        protected static ExpressionParser<T> GetParser<T>(BaseParser orig)
        {
            ExpressionParser<T> newParser = GetParser<T>();

            if (newParser == null)
            {
                throw new NotSupportedException("Unsupported type: " + typeof(T));
            }

            newParser.Init(orig.expression);
            newParser.parseMode = orig.parseMode;
            newParser.currentDataNode = orig.currentDataNode;

            return newParser;
        }

        protected BaseParser GetParser(Type type)
        {
            MethodInfo getParserMethod = GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).
                Where(m => m.Name == "GetParser").Single();
            getParserMethod = getParserMethod.MakeGenericMethod(new Type[] { type });

            return (BaseParser)getParserMethod.Invoke(null, new object[] { this });
        }
        
        protected static Dictionary<string, List<Function>> globalFunctions = new Dictionary<string, List<Function>>();
        protected int readyForCast = 0;
        public string expression;
        protected bool parseMode = true;
        protected DataNode currentDataNode = null;

        /// <summary>
        /// Initialize for parsing.
        /// </summary>
        /// <param name="expression">Expression being parsed</param>
        protected void Init(string expression)
        {
            readyForCast = 0;

            // Create a copy of the expression being parsed
            this.expression = string.Copy(expression);
        }

        /// <summary>
        /// Registers a function that is available globally.
        /// </summary>
        /// <param name="method">The callable function.</param>
        protected static void RegisterGlobalFunction(Function function)
        {
            if (!globalFunctions.ContainsKey(function.Name))
            {
                globalFunctions[function.Name] = new List<Function>();
            }
            globalFunctions[function.Name].Add(function);
        }

    }
}
