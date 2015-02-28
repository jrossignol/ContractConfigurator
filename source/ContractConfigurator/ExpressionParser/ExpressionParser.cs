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
    public abstract class ExpressionParser<T> : BaseParser
    {
        public ExpressionParser()
        {
        }

        protected static Dictionary<string, List<Function>> classMethods = new Dictionary<string, List<Function>>();
        protected static Dictionary<string, List<Function>> classFunctions = new Dictionary<string, List<Function>>();

        /// <summary>
        /// Registers a method that can be called on the given type.
        /// </summary>
        /// <param name="method">The callable method.</param>
        protected static void RegisterMethod(Function method)
        {
            if (!classMethods.ContainsKey(method.Name))
            {
                classMethods[method.Name] = new List<Function>();
            }
            classMethods[method.Name].Add(method);
        }

        /// <summary>
        /// Registers a function that is only available in the contract of the given type.
        /// </summary>
        /// <param name="method">The callable function.</param>
        protected static void RegisterLocalFunction(Function function)
        {
            if (!classFunctions.ContainsKey(function.Name))
            {
                classFunctions[function.Name] = new List<Function>();
            }
            classFunctions[function.Name].Add(function);
        }

        /// <summary>
        /// Gets all available functions under the given name.
        /// </summary>
        /// <param name="name">Name of the function</param>
        /// <returns>Enumeration of functions</returns>
        protected IEnumerable<Function> GetFunctions(string name)
        {
            if (classFunctions.ContainsKey(name))
            {
                foreach (Function f in classFunctions[name])
                {
                    yield return f;
                }
            }
            if (globalFunctions.ContainsKey(name))
            {
                foreach (Function f in globalFunctions[name])
                {
                    yield return f;
                }
            }
        }

        /// <summary>
        /// Executes the given expression.
        /// </summary>
        /// <param name="expression">The expression to execute</param>
        /// <param name="dataNode">The data node that the expression may access</param>
        /// <returns>The result of executing the expression</returns>
        public T ExecuteExpression(string key, string expression, DataNode dataNode)
        {
            T val = default(T);
            try
            {
                parseMode = false;
                currentDataNode = dataNode;
                currentKey = key;
                val = ParseExpression(expression);
            }
            finally
            {
                parseMode = true;
                currentDataNode = null;
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
            LoggingUtil.LogVerbose(typeof(BaseParser), "Parsing expression: " + expression);
            spacing = 0;

            Init(expression);
            try
            {
                return ParseStatement<T>();
            }
            // Let this one flow through so it can be retried
            catch (DataNode.ValueNotInitialized) { throw; }
            catch (Exception e)
            {
                int count = expression.Length - this.expression.Length;
                throw new Exception("Error parsing statement.\nError occurred near '*':\n" +
                    expression + "\n" +
                    (count > 0 ? new String('.', count) : "") + "* <-- HERE", e);
            }
        }

        protected virtual TResult ParseStatement<TResult>()
        {
            LogEntryDebug<TResult>("ParseStatement");

            string savedExpression = expression;
            try
            {
                TResult result = ParseStatementInner<TResult>();
                LogExitDebug<TResult>("ParseStatement", result);
                return result;
            }
            catch (Exception e)
            {
                Type type = GetRequiredType(e);
                if (type == null)
                {
                    throw;
                }

                expression = savedExpression;
                BaseParser altParser = GetParser(type);

                // Call the method on the alternate parser
                MethodInfo method = altParser.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).
                    Where(m => m.Name == "ParseStatementInner").Single();
                method = method.MakeGenericMethod(new Type[] { typeof(TResult) });
                TResult result = (TResult)method.Invoke(altParser, new object[] { });
                expression = altParser.expression;
                LogExitDebug<TResult>("ParseStatement", result);
                return result;
            }
        }

        protected virtual TResult ParseStatementInner<TResult>()
        {
            LogEntryDebug<TResult>("ParseStatementInner");
            T lval = ParseSimpleStatement<T>();
            TResult result = ParseStatement<TResult>(lval);
            LogExitDebug<TResult>("ParseStatementInner", result);
            return result;
        }

        protected TResult ParseStatement<TResult>(T lval)
        {
            LogEntryDebug<TResult>("ParseStatement", lval.ToString());
            ExpressionParser<TResult> parser = GetParser<TResult>(this);

            // End of statement
            TResult result;
            if (expression.Length == 0)
            {
                result = ConvertType<TResult>(lval);
                LogExitDebug<TResult>("ParseStatement", result);
                return result;
            }

            // Get next token
            Token token = ParseToken();

            while (token != null)
            {
                string savedExpression = expression;
                switch (token.tokenType)
                {
                    case TokenType.END_BRACKET:
                    case TokenType.TERNARY_END:
                    case TokenType.COMMA:
                        expression = token.sval + expression;
                        result = ConvertType<TResult>(lval);
                        LogExitDebug<TResult>("ParseStatement", result);
                        return result;
                    case TokenType.OPERATOR:
                        try
                        {
                            lval = ParseOperation<T>(lval, token.sval);
                            break;
                        }
                        catch (Exception e)
                        {
                            Type type = GetRequiredType(e);
                            if (type == null || typeof(T) == typeof(TResult))
                            {
                                throw;
                            }

                            // Parse under the return type
                            expression = savedExpression;
                            TResult val = ParseOperation<TResult>(lval, token.sval);
                            parser.expression = expression;
                            try
                            {
                                result = parser.ParseStatement<TResult>(val);
                                LogExitDebug<TResult>("ParseStatement", result);
                                return result;
                            }
                            finally
                            {
                                expression = parser.expression;
                            }
                        }
                    case TokenType.TERNARY_START:
                        lval = ParseTernary<T>(ConvertType<bool>(lval));
                        break;
                    default:
                        expression = token.sval + expression;
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }

                // Get next token
                token = ParseToken();
            }

            result = ConvertType<TResult>(lval);
            LogExitDebug<TResult>("ParseStatement", result);
            return result;
        }

        protected TResult ParseSimpleStatement<TResult>()
        {
            LogEntryDebug<TResult>("ParseSimpleStatement");

            // Get a token
            Token token = ParseToken();

            ExpressionParser<TResult> parser = GetParser<TResult>(this);

            try
            {
                TResult result;
                switch (token.tokenType)
                {
                    case TokenType.START_BRACKET:
                        result = ParseStatement<TResult>();
                        ParseToken(")");
                        LogExitDebug<TResult>("ParseSimpleStatement", result);
                        return result;
                    case TokenType.IDENTIFIER:
                        result = parser.ParseIdentifier(token);
                        LogExitDebug<TResult>("ParseSimpleStatement", result);
                        return result;
                    case TokenType.FUNCTION:
                        result = parser.ParseFunction(token);
                        LogExitDebug<TResult>("ParseSimpleStatement", result);
                        return result;
                    case TokenType.SPECIAL_IDENTIFIER:
                        result = parser.ParseSpecialIdentifier(token);
                        LogExitDebug<TResult>("ParseSimpleStatement", result);
                        return result;
                    case TokenType.OPERATOR:
                        switch (token.sval)
                        {
                            case "-":
                                {
                                    TResult value = ParseSimpleStatement<TResult>();
                                    parser.expression = expression;
                                    result = parser.Negate(value);
                                    LogExitDebug<TResult>("ParseSimpleStatement", result);
                                    return result;
                                }
                            case "!":
                                {
                                    TResult value = ParseSimpleStatement<TResult>();
                                    parser.expression = expression;
                                    result = parser.Not(value);
                                    LogExitDebug<TResult>("ParseSimpleStatement", result);
                                    return result;
                                }
                            default:
                                expression = token.sval + expression;
                                throw new ArgumentException("Unexpected operator: " + token.sval);
                        }
                    case TokenType.VALUE:
                        result = ConvertType<TResult>((token as ValueToken<T>).val);
                        LogExitDebug<TResult>("ParseSimpleStatement", result);
                        return result;
                    default:
                        expression = token.sval + expression;
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }
            }
            finally
            {
                expression = parser.expression;
            }
        }

        private TResult ParseOperation<TResult>(T lval, string op)
        {
            LogEntryDebug<TResult>("ParseOperation", lval.ToString(), op);

            // Get the right side of the operation
            T rval = GetRval();

            // Get a token
            Token token = ParseToken();

            TResult result;
            while (token != null)
            {
                switch (token.tokenType)
                {
                    case TokenType.END_BRACKET:
                    case TokenType.TERNARY_END:
                    case TokenType.COMMA:
                        expression = token.sval + expression;
                        result = ApplyOperator<TResult>(lval, op, rval);
                        LogExitDebug<TResult>("ParseOperation", result);
                        return result;
                    case TokenType.TERNARY_START:
                        result = ParseTernary<TResult>(ApplyOperator<bool>(lval, op, rval));
                        LogExitDebug<TResult>("ParseOperation", result);
                        return result;
                    case TokenType.OPERATOR:
                        if (precedence[op] >= precedence[token.sval])
                        {
                            expression = token.sval + expression;
                            result = ApplyOperator<TResult>(lval, op, rval);
                            LogExitDebug<TResult>("ParseOperation", result);
                            return result;
                        }
                        else
                        {
                            rval = ParseOperation<T>(rval, token.sval);
                            token = ParseToken();
                        }
                        break;
                    default:
                        expression = token.sval + expression;
                        throw new ArgumentException("Unexpected value: " + token.sval);
                }
            }

            result = ApplyOperator<TResult>(lval, op, rval);
            LogExitDebug<TResult>("ParseOperation", result);
            return result;
        }

        private T GetRval()
        {
            string savedExpression = expression;
            try
            {
                return ParseSimpleStatement<T>();
            }
            catch (Exception e)
            {
                expression = savedExpression;
                Type type = GetRequiredType(e);
                if (type == null || type == typeof(T))
                {
                    throw;
                }

                BaseParser altParser = GetParser(type);

                // Call the method on the alternate parser
                MethodInfo method = altParser.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).
                    Where(m => m.Name == "ParseStatementInner").Single();
                method = method.MakeGenericMethod(new Type[] { typeof(T) });
                T result = (T)method.Invoke(altParser, new object[] { });
                expression = altParser.expression;
                return result;
            }
        }

        protected TResult ParseTernary<TResult>(bool lval)
        {
            TResult val1 = ParseStatement<TResult>();
            ParseToken(":");
            TResult val2 = ParseStatement<TResult>();

            return lval ? val1 : val2;
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
                case ',':
                    expression = expression.Substring(1);
                    return new Token(TokenType.COMMA);
                case '?':
                    expression = expression.Substring(1);
                    return new Token(TokenType.TERNARY_START);
                case ':':
                    expression = expression.Substring(1);
                    return new Token(TokenType.TERNARY_END);
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
                case '@':
                    return ParseSpecialIdentifier();
                case '.':
                    return ParseMethod();
            }

            // Try to parse an identifier
            if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z')
            {
                return ParseIdentifier();
            }

            throw new ArgumentException("Expected a valid expression, found: '" + c + "'");
        }

        protected Token ParseMethodToken()
        {
            Token token = ParseToken();

            if (token == null)
            {
                return null;
            }

            if (token.tokenType == TokenType.METHOD)
            {
                return token;
            }

            expression = token.sval + expression;
            return null;
        }

        protected Token ParseMethodEndToken()
        {
            Token token = ParseToken();

            if (token == null)
            {
                return null;
            }

            if (token.tokenType == TokenType.END_BRACKET || token.tokenType == TokenType.COMMA)
            {
                return token;
            }
            else
            {
                expression = token.sval + expression;
                return null;
            }
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

        protected T _ParseMethod<U>(Token token, U obj)
        {
            ExpressionParser<U> parser = GetParser<U>(this);
            try
            {
                T result = parser.ParseMethod<T>(token, obj);
                return result;
            }
            finally
            {
                expression = parser.expression;
            }
        }

        protected TResult ParseMethod<TResult>(Token token, T obj, bool isFunction = false)
        {
            LogEntryDebug<TResult>("ParseMethod", token.sval, obj != null ? obj.ToString() : "null", isFunction.ToString());
            IEnumerable<Function> methods = isFunction ? GetFunctions(token.sval) : classMethods[token.sval].ToList();

            if (!methods.Any())
            {
                throw new MissingMethodException("Cannot find " + (isFunction ? "function" : "method") + " '" + token.sval + "' for class '" + typeof(T).Name + "'.");
            }

            // Start with method call
            ParseToken("(");

            List<object> parameters = new List<object>();
            Function selectedMethod = null;

            while (true)
            {
                // Get some basic statistics
                int minParam = int.MaxValue;
                int maxParam = 0;
                List<Type> paramTypes = new List<Type>();
                foreach (Function method in methods)
                {
                    int paramCount = method.ParameterCount();
                    minParam = Math.Min(minParam, paramCount);
                    maxParam = Math.Max(maxParam, paramCount);
                    for (int j = 0; j < paramCount; j++)
                    {
                        if (paramTypes.Count <= j)
                        {
                            paramTypes.Add(method.ParameterType(j));
                        }
                        else if (paramTypes[j] != method.ParameterType(j))
                        {
                            paramTypes[j] = null;
                        }
                    }
                }

                // Try to end it
                Token endToken = ParseMethodEndToken();
                if (endToken != null)
                {
                    // End statement
                    if (endToken.tokenType == TokenType.END_BRACKET)
                    {
                        // Find the method that matched
                        foreach (Function method in methods)
                        {
                            int paramCount = method.ParameterCount();
                            if (paramCount == parameters.Count)
                            {
                                bool found = true;
                                for (int j = 0; j < paramCount; j++)
                                {
                                    if (parameters[j].GetType() != method.ParameterType(j))
                                    {
                                        found = false;
                                    }
                                }

                                if (found)
                                {
                                    selectedMethod = method;
                                    break;
                                }
                            }
                        }

                        if (selectedMethod != null)
                        {
                            break;
                        }

                        // End bracket, but no matching method!
                        throw new MethodMismatch(methods);
                    }
                    else if (endToken.tokenType == TokenType.COMMA)
                    {
                        if (parameters.Count() == 0)
                        {
                            throw new ArgumentException("Expected " + (minParam == 0 ? "')'" : "an expression") + ", got: ','.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Expected ')', got: " + endToken.sval);
                    }
                }
                else if (parameters.Count() != 0)
                {
                    token = ParseToken();
                    throw new ArgumentException("Expected ',', got: " + token.sval);
                }

                // Check for end of statement
                if (expression.Trim() == "")
                {
                    throw new ArgumentException("Expected an expression, got end of statement");
                }

                Type paramType = paramTypes[parameters.Count];
                // Easy - we have the type!
                if (paramType != null)
                {
                    BaseParser parser = GetParser(paramType);

                    try
                    {
                        MethodInfo method = parser.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).
                            Where(m => m.Name == "ParseStatement" && m.GetParameters().Count() == 0).Single();
                        method = method.MakeGenericMethod(new Type[] { paramType });
                        object value = method.Invoke(parser, new object[] { });
                        parameters.Add(value);
                    }
                    finally
                    {
                        expression = parser.expression;
                    }
                }
                else
                {
                    // TODO - implement once there's a use case for more complex overloading
                }
            }

            // Add object to the parameter list
            if (!isFunction)
            {
                List<object> newParam = new List<object>();
                newParam.Add(obj);
                newParam.AddRange(parameters);
                parameters = newParam;
            }

            // Invoke the method
            object result = selectedMethod.Invoke(parameters.ToArray());

            if (!selectedMethod.Deterministic && currentDataNode != null)
            {
                currentDataNode.SetDeterministic(currentKey, false);
            }

            // Check for a method call before we return
            Token methodToken = ParseMethodToken();
            ExpressionParser<TResult> retValParser = GetParser<TResult>(this);
            if (methodToken != null)
            {
                MethodInfo parseMethod = retValParser.GetType().GetMethod("_ParseMethod", BindingFlags.NonPublic | BindingFlags.Instance);
                parseMethod = parseMethod.MakeGenericMethod(new Type[] { result.GetType() });

                result = parseMethod.Invoke(retValParser, new object[] { methodToken, result });
                expression = retValParser.expression;
            }

            // No method, return the result
            TResult retVal = retValParser.ConvertType(result);
            LogExitDebug<TResult>("ParseOperation", retVal);
            return retVal;
        }

        protected T ParseFunction(Token token)
        {
            return ParseMethod<T>(token, default(T), true);
        }

        /// <summary>
        /// Parses an identifier for a config node value.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the config node identifier</returns>
        protected virtual T ParseSpecialIdentifier(Token token)
        {
            if (currentDataNode != null)
            {
                if (!currentDataNode.IsInitialized(token.sval))
                {
                    throw new DataNode.ValueNotInitialized(token.sval);
                }

                object o = currentDataNode[token.sval];
                if (!currentDataNode.IsDeterministic(token.sval))
                {
                    currentDataNode.SetDeterministic(currentKey, false);
                }

                // Check for a method call before we start messing with types
                Token methodToken = ParseMethodToken();
                if (methodToken != null)
                {
                    MethodInfo parseMethod = GetType().GetMethod("_ParseMethod", BindingFlags.NonPublic | BindingFlags.Instance);
                    parseMethod = parseMethod.MakeGenericMethod(new Type[] { o.GetType() });

                    return (T)parseMethod.Invoke(this, new object[] { methodToken, o });
                }

                // No method, try type conversion or straight return
                if (o.GetType() == typeof(T))
                {
                    return (T)o;
                }
                else
                {
                    return ConvertType(o);
                }
            }
            else
            {
                throw new ArgumentException("Cannot get value for @" + token.sval + ": not available in this context.");
            }
        }

        protected Token ParseIdentifier()
        {
            Match m = Regex.Match(expression, "([A-Za-z][A-Za-z0-9_]*).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");

            expression.Trim();
            TokenType type = expression.Length > 0 && expression.Substring(0, 1) == "(" ? TokenType.FUNCTION : TokenType.IDENTIFIER;

            return new Token(type, identifier);
        }

        protected Token ParseSpecialIdentifier()
        {
            Match m = Regex.Match(expression, "@([A-Za-z][A-Za-z0-9_]*).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length + 1 ? expression.Substring(identifier.Length + 1) : "");

            return new Token(TokenType.SPECIAL_IDENTIFIER, identifier);
        }

        protected Token ParseMethod()
        {
            Match m = Regex.Match(expression, "\\.([A-Za-z][A-Za-z0-9_]*).*");
            string identifier = m.Groups[1].Value;

            if (string.IsNullOrEmpty(identifier))
            {
                return null;
            }

            expression = (expression.Length > identifier.Length + 1 ? expression.Substring(identifier.Length + 1) : "");

            return new Token(TokenType.METHOD, identifier);
        }


        protected virtual Token ParseNumericConstant()
        {
            throw new WrongDataType(typeof(double), typeof(T));
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

        protected TResult ApplyOperator<TResult>(T lval, string op, T rval)
        {
            if (IsBoolean(op))
            {
                if (typeof(TResult) == typeof(bool) && IsBoolean(op))
                {
                    return (TResult)(object)ApplyBooleanOperator(lval, op, rval);
                }
                else
                {
                    throw new DataStoreCastException(typeof(bool), typeof(T));
                }
            }

            switch (op)
            {
                case "+":
                    return ConvertType<TResult>(Add(lval, rval));
                case "-":
                    return ConvertType<TResult>(Sub(lval, rval));
                case "*":
                    return ConvertType<TResult>(Mult(lval, rval));
                case "/":
                    return ConvertType<TResult>(Div(lval, rval));
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

        /// <summary>
        /// Performs a data type conversion.
        /// </summary>
        /// <typeparam name="U">Type to convert to.</typeparam>
        /// <param name="value">Value to convert from.</param>
        /// <returns>The converted value.</returns>
        protected virtual U ConvertType<U>(T value)
        {
            // Probably should never happen, but handle the basic case
            if (typeof(T) == typeof(U))
            {
                return (U)(object)value;
            }

            // Try basic conversion
            try
            {
                return (U)Convert.ChangeType(value, typeof(U));
            }
            catch
            {
                throw new DataStoreCastException(typeof(T), typeof(U));
            }
        }

        protected T ConvertType(object value)
        {
            MethodInfo convertMethod = GetType().GetMethod("_ConvertType", BindingFlags.NonPublic | BindingFlags.Instance);
            convertMethod = convertMethod.MakeGenericMethod(new Type[] { value.GetType() });

            try
            {
                T result = (T)convertMethod.Invoke(this, new object[] { value });
                return result;
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException.GetType() == typeof(DataStoreCastException))
                {
                    DataStoreCastException orig = (DataStoreCastException)e.InnerException;
                    throw new DataStoreCastException(orig.FromType, orig.ToType, e);
                }
                throw;
            }
        }

        protected T _ConvertType<U>(U value)
        {
            ExpressionParser<U> parser = GetParser<U>(this);
            return parser.ConvertType<T>(value);
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
