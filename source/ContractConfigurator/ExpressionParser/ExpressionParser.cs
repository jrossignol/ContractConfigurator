using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Class for parsing an expression
    /// </summary>
    /// <typeparam name="T">The type of value to return for the expression.</typeparam>
    public abstract class ExpressionParser<T>
    {
        protected string expression;
        protected bool parseMode = true;
        protected int readyForCast = 0;

        public ExpressionParser()
        {
        }

        protected static ExpressionParser<T> GetParser<U>(ExpressionParser<U> orig)
        {
            ExpressionParser<T> newParser = ExpressionParserUtil.GetParser<T>();

            if (newParser == null)
            {
                throw new NotSupportedException("Unsupported type: " + typeof(T));
            }

            newParser.Init(orig.expression);
            newParser.parseMode = orig.parseMode;

            return newParser;
        }

        /// <summary>
        /// Initialize for parsing.
        /// </summary>
        /// <param name="expression">Expression being parsed</param>
        protected void Init(string expression)
        {
            // Create a copy of the expression being parsed
            this.expression = string.Copy(expression);
        }

        /// <summary>
        /// Executes the given expression.
        /// </summary>
        /// <param name="expression">The expression to execute</param>
        /// <returns>The result of executing the expression</returns>
        public T ExecuteExpression(string expression)
        {
            T val = default(T);
            try
            {
                readyForCast = 0;
                parseMode = false;
                val = ParseExpression(expression);
            }
            finally
            {
                parseMode = true;
            }

            return val;
        }

        /// <summary>
        /// Executes the given expression in parse mode.
        /// </summary>
        /// <param name="expression">The expression to parse</param>
        /// <returns>The result of parsing the expression</returns>
        public T ParseExpression(string expression)
        {
            Init(expression);
            try
            {
                return ParseStatement();
            }
            catch (Exception e)
            {
                throw new Exception("Error parsing statement.\nError occurred near '*':\n" +
                    this.expression + "\n" +
                    new String(' ', expression.Length - this.expression.Length) + "* <-- HERE", e);
            }
        }

        protected T ParseAlternateStatement<U>()
        {
            ExpressionParser<U> parser = ExpressionParser<U>.GetParser<T>(this);

            U lval = parser.ParseSimpleStatement();

            // End of statement
            if (parser.expression.Length == 0)
            {
                // Attempt to convert type
                expression = parser.expression;
                return (T)Convert.ChangeType(lval, typeof(U));
            }

            // Get next token
            Token token = parser.ParseToken();

            while (token != null)
            {
                switch (token.tokenType)
                {
                    case TokenType.START_BRACKET:
                        expression = parser.expression;
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.END_BRACKET:
                        parser.expression = ")" + parser.expression;
                        // Attempt to convert type
                        return (T)Convert.ChangeType(lval, typeof(U));
                    case TokenType.IDENTIFIER:
                    case TokenType.VALUE:
                        expression = parser.expression;
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.OPERATOR:
                        if (typeof(T) == typeof(bool) && IsBoolean(token.sval))
                        {
                            T lvalT = (T)(object)parser.ParseBooleanOperation(lval, token.sval);
                            expression = parser.expression;
                            return ParseStatement(lvalT);
                        }
                        else
                        {
                            lval = parser.ParseOperation(lval, token.sval);
                            expression = parser.expression;
                            break;
                        }
                    default:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }

                // Get next token
                token = parser.ParseToken();
            }

            // Attempt to convert type
            return (T)Convert.ChangeType(lval, typeof(U));
        }

        protected T ParseAlternateStatementWithLval<U>(T lval)
        {
            ExpressionParser<U> parser = ExpressionParser<U>.GetParser<T>(this);

            // Get next token
            Token token = parser.ParseToken();

            while (token != null)
            {
                switch (token.tokenType)
                {
                    case TokenType.START_BRACKET:
                        expression = parser.expression;
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.END_BRACKET:
                        parser.expression = ")" + parser.expression;
                        expression = parser.expression;
                        return lval;
                    case TokenType.IDENTIFIER:
                    case TokenType.VALUE:
                        expression = parser.expression;
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.OPERATOR:
                        lval = parser.ParseOperation<T>(lval, token.sval);
                        expression = parser.expression;
                        break;
                    default:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }

                // Get next token
                token = parser.ParseToken();
            }

            expression = parser.expression;
            return lval;
        }

        protected T ParseStatement()
        {
            string savedExpression = expression;
            try
            {
                readyForCast++;
                T lval = ParseSimpleStatement();
                lval = ParseStatement(lval);
                readyForCast--;
                return lval;
            }
            catch (DataStoreCastException e)
            {
                if (--readyForCast != 0)
                {
                    throw;
                }

                // Create the generic methods
                MethodInfo parseMethod = GetType().GetMethod("ParseAlternateStatement", BindingFlags.Instance | BindingFlags.NonPublic);
                parseMethod = parseMethod.MakeGenericMethod(new Type[] { e.FromType });

                expression = savedExpression;
                return (T)parseMethod.Invoke(this, null);
            }
            catch (NotSupportedException)
            {
                if (--readyForCast != 0)
                {
                    throw;
                }

                expression = savedExpression;
                return ParseAlternateStatement<double>();
            }
        }

        protected T ParseStatement(T lval)
        {
            // End of statement
            if (expression.Length == 0)
            {
                return lval;
            }

            string savedExpression = expression;
            try
            {
                readyForCast++;

                // Get next token
                Token token = ParseToken();

                while (token != null)
                {
                    switch (token.tokenType)
                    {
                        case TokenType.START_BRACKET:
                            throw new ArgumentException("Unexpected value: " + token.sval);
                        case TokenType.END_BRACKET:
                            expression = ")" + expression;
                            return lval;
                        case TokenType.IDENTIFIER:
                        case TokenType.VALUE:
                            throw new ArgumentException("Unexpected value: " + token.sval);
                        case TokenType.OPERATOR:
                            lval = ParseOperation(lval, token.sval);
                            break;
                        default:
                            throw new ArgumentException("Unexpected value: " + token.sval);
                    }

                    // Get next token
                    token = ParseToken();
                }

                readyForCast--;
                return lval;
            }
            catch (DataStoreCastException e)
            {
                if (--readyForCast != 0)
                {
                    throw;
                }

                // Create the generic methods
                MethodInfo parseMethod = GetType().GetMethod("ParseAlternateStatementWithLval", BindingFlags.Instance | BindingFlags.NonPublic);
                parseMethod = parseMethod.MakeGenericMethod(new Type[] { e.FromType });

                expression = savedExpression;
                return (T)parseMethod.Invoke(this, new object[] { lval });
            }
            catch (NotSupportedException)
            {
                if (--readyForCast != 0)
                {
                    throw;
                }

                expression = savedExpression;
                return ParseAlternateStatementWithLval<double>(lval);
            }

        }

        protected T ParseSimpleStatement()
        {
            string savedExpression = expression;
            try
            {
                readyForCast++;
                T lval = ParseSimpleStatementInner();
                readyForCast--;
                return lval;
            }
            catch (DataStoreCastException e)
            {
                if (--readyForCast != 0)
                {
                    throw;
                }

                // Create the generic methods
                MethodInfo parseMethod = GetType().GetMethod("ParseAlternateSimpleStatement", BindingFlags.Instance | BindingFlags.NonPublic);
                parseMethod = parseMethod.MakeGenericMethod(new Type[] { e.FromType });

                expression = savedExpression;
                return (T)parseMethod.Invoke(this, null);
            }
            catch (NotSupportedException)
            {
                if (--readyForCast != 0)
                {
                    throw;
                }

                expression = savedExpression;
                return ParseAlternateSimpleStatement<double>();
            }
        }

        protected T ParseAlternateSimpleStatement<U>()
        {
            ExpressionParser<U> parser = ExpressionParser<U>.GetParser<T>(this);

            // Get a token
            Token token = parser.ParseToken();

            U lval;
            switch (token.tokenType)
            {
                case TokenType.START_BRACKET:
                    lval = parser.ParseStatement();
                    ParseToken(")");
                    break;
                case TokenType.IDENTIFIER:
                    lval = parser.ParseIdentifier(token);
                    break;
                case TokenType.OPERATOR:
                    switch (token.sval)
                    {
                        case "-":
                            lval = parser.Negate(parser.ParseSimpleStatement());
                            break;
                        case "!":
                            lval = parser.Not(parser.ParseSimpleStatement());
                            break;
                        default:
                            throw new ArgumentException("Unexpected operator: " + token.sval);
                    }
                    break;
                case TokenType.VALUE:
                    lval = (token as ValueToken<U>).val;
                    break;
                default:
                    throw new ArgumentException("Unexpected value: " + token.sval);
            }

            // Attempt to convert type
            expression = parser.expression;
            return (T)Convert.ChangeType(lval, typeof(U));
        }

        protected T ParseSimpleStatementInner()
        {
            // Get a token
            Token token = ParseToken();

            switch (token.tokenType)
            {
                case TokenType.START_BRACKET:
                    T val = ParseStatement();
                    ParseToken(")");
                    return val;
                case TokenType.IDENTIFIER:
                    return ParseIdentifier(token);
                case TokenType.OPERATOR:
                    switch (token.sval)
                    {
                        case "-":
                            return Negate(ParseSimpleStatement());
                        case "!":
                            return Not(ParseSimpleStatement());
                        default:
                            throw new ArgumentException("Unexpected operator: " + token.sval);
                    }
                case TokenType.VALUE:
                    return (token as ValueToken<T>).val;
                default:
                    throw new ArgumentException("Unexpected value: " + token.sval);
            }
        }

        protected Token ParseToken()
        {
            expression = expression.Trim();

            if (expression.Length == 0)
            {
                return null;
            }

            char c = expression.Substring(0, 1).ToCharArray()[0];
            switch (c)
            {
                case '(':
                    expression = expression.Substring(1);
                    return new Token(TokenType.START_BRACKET);
                case ')':
                    expression = expression.Substring(1);
                    return new Token(TokenType.END_BRACKET);
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '0':
                    return ParseNumericConstant();
                case '|':
                case '&':
                case '+':
                case '-':
                case '!':
                case '<':
                case '>':
                case '=':
                case '*':
                case '/':
                    return ParseOperator();
            }

            // Try to parse an identifier
            if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z')
            {
                return ParseIdentifier();
            }

            throw new ArgumentException("Expected a valid expression, found: '" + c + "'");
        }

        protected void ParseToken(string expected)
        {
            Token token = ParseToken();
            if (token.sval != expected)
            {
                throw new ArgumentException("Expected '" + expected + "', got: " + token.sval);
            }
        }

        /// <summary>
        /// Parses an identifier for a value stored in the persistant data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the identifier</returns>
        protected virtual T ParseIdentifier(Token token)
        {
            throw new NotSupportedException("Can't parse identifier for type " + typeof(T) + " in class " + this.GetType() + " - not supported!");
        }

        protected Token ParseIdentifier()
        {
            Match m = Regex.Match(expression, "([A-Za-z][A-Za-z0-9_]*).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");

            return new Token(TokenType.IDENTIFIER, identifier);
        }

        private T ParseOperation(T lval, string op)
        {
            T rval = ParseSimpleStatement();

            // Get a token
            Token token = ParseToken();

            while (token != null)
            {
                switch (token.tokenType)
                {
                    case TokenType.START_BRACKET:
                    case TokenType.IDENTIFIER:
                    case TokenType.VALUE:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.END_BRACKET:
                        expression = token.sval + expression;
                        return ApplyOperator(lval, op, rval);
                    case TokenType.OPERATOR:
                        if (ExpressionParserUtil.precedence[op] >= ExpressionParserUtil.precedence[token.sval])
                        {
                            expression = token.sval + expression;
                            return ApplyOperator(lval, op, rval);
                        }
                        else
                        {
                            rval = ParseOperation(rval, token.sval);
                            token = ParseToken();
                        }
                        break;
                    default:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }
            }

            return ApplyOperator(lval, op, rval);
        }

        private U ParseOperation<U>(U lval, string op)
        {
            T rval = ParseSimpleStatement();

            // Get a token
            Token token = ParseToken();

            while (token != null)
            {
                switch (token.tokenType)
                {
                    case TokenType.START_BRACKET:
                    case TokenType.IDENTIFIER:
                    case TokenType.VALUE:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.END_BRACKET:
                        expression = token.sval + expression;
                        throw new Exception("ackbar");
                    //return ApplyOperator(lval, op, rval);
                    case TokenType.OPERATOR:
                        if (ExpressionParserUtil.precedence[op] >= ExpressionParserUtil.precedence[token.sval])
                        {
                            expression = token.sval + expression;
                            throw new Exception("backbar");
                            //return ApplyOperator(lval, op, rval);
                        }
                        else
                        {
                            if (typeof(U) == typeof(Boolean) && IsBoolean(token.sval))
                            {
                                return (U)(object)ParseBooleanOperation(rval, token.sval);
                            }
                            else
                            {
                                rval = ParseOperation(rval, token.sval);
                                token = ParseToken();
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }

            }

            throw new Exception("Cackbar");
//            return ApplyOperator(lval, op, rval);
        }

        private bool ParseBooleanOperation(T lval, string op)
        {
            T rval = ParseSimpleStatement();

            // Get a token
            Token token = ParseToken();

            return ParseBooleanOperation(lval, op, token, rval);
        }
        private bool ParseBooleanOperation(T lval, string op, Token token, T rval)
        {
            while (token != null)
            {
                switch (token.tokenType)
                {
                    case TokenType.START_BRACKET:
                    case TokenType.IDENTIFIER:
                    case TokenType.VALUE:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.END_BRACKET:
                        expression = token.sval + expression;
                        return ApplyBooleanOperator(lval, op, rval);
                    case TokenType.OPERATOR:
                        if (ExpressionParserUtil.precedence[op] >= ExpressionParserUtil.precedence[token.sval])
                        {
                            expression = token.sval + expression;
                            return ApplyBooleanOperator(lval, op, rval);
                        }
                        else
                        {
                            rval = ParseOperation(rval, token.sval);
                            token = ParseToken();
                        }
                        break;
                    default:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }
            }

            return ApplyBooleanOperator(lval, op, rval);
        }

        protected virtual Token ParseNumericConstant()
        {
            throw new NotSupportedException("Numeric constants not supported for type " + typeof(T));
        }

        protected Token ParseOperator()
        {
            char[] chars = expression.Substring(0, 2).ToCharArray();
            switch (chars[0])
            {
                case '|':
                    return ParseOperator("||");
                case '&':
                    return ParseOperator("&&");
                case '-':
                case '+':
                case '*':
                case '/':
                    return ParseOperator(new string(chars[0], 1));
                case '!':
                    switch (chars[1])
                    {
                        case '=':
                            return ParseOperator("!=");
                        default:
                            return ParseOperator("!");
                    }
                case '<':
                    switch (chars[1])
                    {
                        case '=':
                            return ParseOperator("<=");
                        default:
                            return ParseOperator("<");
                    }
                case '>':
                    switch (chars[1])
                    {
                        case '=':
                            return ParseOperator(">=");
                        default:
                            return ParseOperator(">");
                    }
                case '=':
                    return ParseOperator("==");
            }

            throw new ArgumentException("Expected an operator, found: " + expression.Substring(0, 2));
        }

        protected Token ParseOperator(string op)
        {
            if (expression.Substring(0, op.Length) == op)
            {
                expression = (expression.Length > op.Length ? expression.Substring(op.Length) : "");
                return new Token(TokenType.OPERATOR, op);
            }
            else
            {
                throw new ArgumentException("Expected '" + op + "', found: " + expression.Substring(0, op.Length));
            }
        }

        protected T ApplyOperator(T lval, string op, T rval)
        {
            if (IsBoolean(op))
            {
                if (typeof(T) == typeof(bool) && IsBoolean(op))
                {
                    return (T)(object)ApplyBooleanOperator(lval, op, rval);
                }
                else
                {
                    throw new DataStoreCastException(typeof(bool), typeof(T));
                }
            }

            switch (op)
            {
                case "+":
                    return Add(lval, rval);
                case "-":
                    return Sub(lval, rval);
                case "*":
                    return Mult(lval, rval);
                case "/":
                    return Div(lval, rval);
                default:
                    throw new ArgumentException("Unexpected operator:  '" + op);
            }
        }

        protected bool ApplyBooleanOperator(T lval, string op, T rval)
        {
            switch (op)
            {
                case "||":
                    return Or(lval, rval);
                case "&&":
                    return And(lval, rval);
                case "<":
                    return LT(lval, rval);
                case "<=":
                    return LE(lval, rval);
                case "==":
                    return EQ(lval, rval);
                case "!=":
                    return NE(lval, rval);
                case ">":
                    return GT(lval, rval);
                case ">=":
                    return GE(lval, rval);
                default:
                    throw new ArgumentException("Unexpected operator:  '" + op);
            }
        }

        protected bool IsBoolean(string op)
        {
            string[] booleans = { "!", "||", "&&", "<", "<=", "==", "!=", ">", ">=" };
            return booleans.Contains(op);
        }

        protected virtual T Negate(T val)
        {
            throw new NotSupportedException("Negation (-) not supported for type " + typeof(T));
        }

        protected virtual T Add(T a, T b)
        {
            throw new NotSupportedException("Addition (+) not supported for type " + typeof(T));
        }

        protected virtual T Sub(T a, T b)
        {
            throw new NotSupportedException("Subtraction (-) not supported for type " + typeof(T));
        }

        protected virtual T Mult(T a, T b)
        {
            throw new NotSupportedException("Multiplication (*) not supported for type " + typeof(T));
        }

        protected virtual T Div(T a, T b)
        {
            throw new NotSupportedException("Division (/) not supported for type " + typeof(T));
        }

        protected virtual T Not(T val)
        {
            throw new NotSupportedException("Logical NOT (!) is not supported for type " + typeof(T));
        }

        protected virtual bool Or(T a, T b)
        {
            throw new NotSupportedException("Logical OR (||) not supported for type " + typeof(T));
        }

        protected virtual bool And(T a, T b)
        {
            throw new NotSupportedException("Logical AND (&&) not supported for type " + typeof(T));
        }

        protected virtual bool LT(T a, T b)
        {
            throw new NotSupportedException("Less than (<) not supported for type " + typeof(T));
        }

        protected virtual bool LE(T a, T b)
        {
            throw new NotSupportedException("Less than or equal (<=) not supported for type " + typeof(T));
        }

        protected virtual bool EQ(T a, T b)
        {
            throw new NotSupportedException("Equal (==) not supported for type " + typeof(T));
        }

        protected virtual bool NE(T a, T b)
        {
            throw new NotSupportedException("Equal (==) not supported for type " + typeof(T));
        }

        protected virtual bool GE(T a, T b)
        {
            throw new NotSupportedException("Greater than or equal (>=) not supported for type " + typeof(T));
        }

        protected virtual bool GT(T a, T b)
        {
            throw new NotSupportedException("Greater than (>) not supported for type " + typeof(T));
        }
    }
}
