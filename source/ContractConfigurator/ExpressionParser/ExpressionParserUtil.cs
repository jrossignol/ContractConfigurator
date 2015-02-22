using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.ExpressionParser
{
    public enum TokenType
    {
        IDENTIFIER,
        VALUE,
        OPERATOR,
        START_BRACKET,
        END_BRACKET
    }

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

    public static class ExpressionParserUtil
    {
        private static Dictionary<Type, Type> parserTypes = new Dictionary<Type, Type>();

        static ExpressionParserUtil()
        {
            // Register expression parsers
            RegisterParserType(typeof(bool), typeof(BooleanValueExpressionParser));
            RegisterParserType(typeof(uint), typeof(NumericValueExpressionParser<uint>));
            RegisterParserType(typeof(int), typeof(NumericValueExpressionParser<int>));
            RegisterParserType(typeof(float), typeof(NumericValueExpressionParser<float>));
            RegisterParserType(typeof(double), typeof(NumericValueExpressionParser<double>));
            RegisterParserType(typeof(string), typeof(ComparableClassExpressionParser<string>));
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
