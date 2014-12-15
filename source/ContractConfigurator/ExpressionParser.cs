using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    public class ExpressionParser
    {
        private enum TokenType
        {
            IDENTIFIER,
            VALUE,
            OPERATOR,
            START_BRACKET,
            END_BRACKET
        }

        private class Token
        {
            public TokenType tokenType;
            public string sval;
            public double dval;

            public Token(double d)
            {
                tokenType = TokenType.VALUE;
                dval = d;
                sval = d.ToString();
            }

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

        private static char[] WHITESPACE_OR_OPERATOR =
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
        private static Dictionary<string, int> precedence = new Dictionary<string, int>();
        private static string expression;
        private static bool parseMode = true;

        public static void ExecuteExpression(string key, string expression)
        {
            PersistentDataStore.Instance.Store<double>(key, ExecuteExpression(expression));
        }

        public static double ExecuteExpression(string expression)
        {
            parseMode = false;
            double val = ParseExpression(expression);
            parseMode = true;

            return val;
        }

        public static double ParseExpression(string expression)
        {
            Init(expression);
            try
            {
                return ParseStatement();
            }
            catch (Exception e)
            {
                throw new Exception("Error parsing statement.\nError occurred near '*':\n" +
                    expression + "\n" +
                    new String(' ', expression.Length - ExpressionParser.expression.Length) + "* <-- HERE", e);
            }
        }

        /*
         * Initialize global structures.
         */
        private static void Init(string expression)
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

            // Create a copy of the expression being parsed
            ExpressionParser.expression = (string)expression.Clone();
        }

        private static double ParseStatement()
        {
            double lval = ParseSimpleStatement();

            // End of statement
            if (expression.Length == 0)
            {
                return lval;
            }

            // Get next token
            Token token = ExpressionParser.ParseToken();

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
                token = ExpressionParser.ParseToken();
            }

            return lval;
        }

        private static double ParseSimpleStatement()
        {
            // Get a token
            Token token = ExpressionParser.ParseToken();

            switch(token.tokenType)
            {
                case TokenType.START_BRACKET:
                    double val = ParseStatement();
                    ParseToken(")");
                    return val;
                case TokenType.IDENTIFIER:
                    return ParseIdentifier(token);
                case TokenType.OPERATOR:
                    switch (token.sval)
                    {
                        case "-":
                            return -ParseSimpleStatement();
                        case "!":
                            return ParseSimpleStatement() == 0.0 ? 1.0 : 0.0;
                        default:
                            throw new ArgumentException("Unexpected operator: " + token.sval);
                    }
                case TokenType.VALUE:
                    return token.dval;
                default:
                    throw new ArgumentException("Unexpected value: " + token.sval);
            }
        }

        private static double ParseIdentifier(Token token)
        {
            return parseMode ? 1.0 : PersistentDataStore.Instance.Retrieve<double>(token.sval);
        }

        private static Token ParseIdentifier()
        {
            Match m = Regex.Match(expression, "([A-Za-z][A-Za-z0-9_]*).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");

            return new Token(TokenType.IDENTIFIER, identifier);
        }
        
        private static double ParseOperation(double lval, string op)
        {
            double rval = ParseSimpleStatement();

            // Get a token
            Token token = ExpressionParser.ParseToken();

            while (token != null)
            {
                switch(token.tokenType)
                {
                    case TokenType.START_BRACKET:
                    case TokenType.IDENTIFIER:
                    case TokenType.VALUE:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                    case TokenType.END_BRACKET:
                        expression = token.sval + expression;
                        return ApplyOperator(lval, op, rval);
                    case TokenType.OPERATOR:
                        if (precedence[op] >= precedence[token.sval])
                        {
                            expression = token.sval + expression;
                            return ApplyOperator(lval, op, rval);
                        }
                        else
                        {
                            rval = ParseOperation(rval, token.sval);
                            token = ExpressionParser.ParseToken();
                        }
                        break;
                    default:
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }
            }

            return ApplyOperator(lval, op, rval);
        }

        private static Token ParseToken()
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
                    return ParseValue();
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

        private static void ParseToken(string expected)
        {
            Token token = ParseToken();
            if (token.sval != expected)
            {
                throw new ArgumentException("Expected '" + expected + "', got: " + token.sval);
            }
        }


        private static Token ParseValue()
        {
            int index = expression.IndexOfAny(WHITESPACE_OR_OPERATOR, 0);

            double val;
            if (index >= 0)
            {
                val = Double.Parse(expression.Substring(0, index));
                expression = (expression.Length > index ? expression.Substring(index) : "");
            }
            else
            {
                val = Double.Parse(expression);
                expression = "";
            }
            return new Token(val);
        }

        private static Token ParseOperator()
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
                            return ParseOperator(">");
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

        private static Token ParseOperator(string op)
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

        private static double ApplyOperator(double lval, string op, double rval)
        {
            switch (op)
            {
                case "||":
                    return (lval != 0.0) || (rval != 0.0) ? 1.0 : 0.0;
                case "&&":
                    return (lval != 0.0) && (rval != 0.0) ? 1.0 : 0.0;
                case "<":
                    return lval < rval ? 1.0 : 0.0;
                case "<=":
                    return lval <= rval ? 1.0 : 0.0;
                case "==":
                    return Math.Abs(lval - rval) < 0.0001 ? 1.0 : 0.0;
                case "!=":
                    return Math.Abs(lval - rval) >= 0.0001 ? 1.0 : 0.0;
                case ">":
                    return lval > rval ? 1.0 : 0.0;
                case ">=":
                    return lval >= rval ? 1.0 : 0.0;
                case "+":
                    return lval + rval;
                case "-":
                    return lval - rval;
                case "*":
                    return lval * rval;
                case "/":
                    return lval / rval;
                default:
                    throw new ArgumentException("Unexpected operator:  '" + op);
            }
        }
    }
}
