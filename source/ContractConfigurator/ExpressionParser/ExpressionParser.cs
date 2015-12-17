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

        public static Dictionary<string, List<Function>> classMethods = new Dictionary<string, List<Function>>();
        public static Dictionary<string, List<Function>> classFunctions = new Dictionary<string, List<Function>>();

        /// <summary>
        /// Registers a method that can be called on the given type.
        /// </summary>
        /// <param name="method">The callable method.</param>
        public static void RegisterMethod(Function method)
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
        public static void RegisterLocalFunction(Function function)
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
        public IEnumerable<Function> GetFunctions(string name)
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
                val = ParseExpression(key, expression, dataNode);
            }
            finally
            {
                parseMode = true;
            }

            return val;
        }

        public override void ExecuteAndStoreExpression(string key, string expression, DataNode dataNode)
        {
            if (PersistentDataStore.Instance != null)
            {
                PersistentDataStore.Instance.Store<T>(key, ExecuteExpression("", expression, dataNode));
            }
            else
            {
                LoggingUtil.LogWarning(this, "Unable to store value for '" + key + "' - PersistentDataStore is null.  This is likely caused by another ScenarioModule crashing, preventing others from loading.");
            }
        }

        public override object ParseExpressionGeneric(string key, string expression, DataNode dataNode)
        {
            return ParseExpression(key, expression, dataNode);
        }


        /// <summary>
        /// Executes the given expression in parse mode.
        /// </summary>
        /// <param name="expression">The expression to parse</param>
        /// <returns>The result of parsing the expression</returns>
        public T ParseExpression(string key, string expression, DataNode dataNode)
        {
            LoggingUtil.LogVerbose(typeof(BaseParser), "Parsing expression: " + expression);
            spacing = 0;

            Init(expression);
            currentKey = key;
            currentDataNode = dataNode;
            tempVariables.Clear();

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
            finally
            {
                currentKey = null;
                currentDataNode = null;
            }
        }

        public virtual TResult ParseStatement<TResult>()
        {
            verbose &= LogEntryDebug<TResult>("ParseStatement");
            try
            {
                string savedExpression = expression;
                try
                {
                    TResult result = ParseStatementInner<TResult>();
                    verbose &= LogExitDebug<TResult>("ParseStatement", result);
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
                    MethodInfo method = altParser.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).
                        Where(m => m.Name == "ParseStatementInner").Single();
                    method = method.MakeGenericMethod(new Type[] { typeof(TResult) });
                    try
                    {
                        TResult result = (TResult)method.Invoke(altParser, new object[] { });
                        verbose &= LogExitDebug<TResult>("ParseStatement", result);
                        return result;
                    }
                    catch (TargetInvocationException tie)
                    {
                        e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                        if (e != null)
                        {
                            throw e;
                        }
                        throw;
                    }
                    finally
                    {
                        expression = altParser.expression;
                    }
                }
            }
            catch
            {
                verbose &= LogException<TResult>("ParseStatement");
                throw;
            }
        }

        public virtual TResult ParseStatementInner<TResult>()
        {
            verbose &= LogEntryDebug<TResult>("ParseStatementInner");
            try
            {
                T lval = ParseSimpleStatement<T>();
                TResult result = ParseStatement<TResult>(lval);
                verbose &= LogExitDebug<TResult>("ParseStatementInner", result);
                return result;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseStatementInner");
                throw;
            }
        }

        public TResult ParseStatement<TResult>(T lval)
        {
            verbose &= LogEntryDebug<TResult>("ParseStatement", lval);
            try
            {
                // End of statement
                TResult result;
                if (expression.Length == 0)
                {
                    result = ConvertType<TResult>(lval);
                    verbose &= LogExitDebug<TResult>("ParseStatement", result);
                    return result;
                }

                // Get next token
                Token token = ParseToken();

                ExpressionParser<TResult> parser = GetParser<TResult>(this);
                while (token != null)
                {
                    string savedExpression = expression;
                    switch (token.tokenType)
                    {
                        case TokenType.END_BRACKET:
                        case TokenType.TERNARY_END:
                        case TokenType.LIST_END:
                        case TokenType.COMMA:
                            expression = token.sval + expression;
                            result = ConvertType<TResult>(lval);
                            verbose &= LogExitDebug<TResult>("ParseStatement", result);
                            return result;
                        case TokenType.METHOD:
                            result = ParseMethod<TResult>(token, lval);
                            parser.expression = expression;
                            try
                            {
                                result = parser.ParseStatement<TResult>(result);
                                verbose &= LogExitDebug<TResult>("ParseStatement", result);
                                return result;
                            }
                            finally
                            {
                                expression = parser.expression;
                            }
                        case TokenType.OPERATOR:
                            try
                            {
                                // Parse under the return type
                                TResult val = ParseOperation<TResult>(lval, token.sval);
                                parser.expression = expression;
                                if (string.IsNullOrEmpty(expression))
                                {
                                    result = val;
                                }
                                else
                                {
                                    result = parser.ParseStatement<TResult>(val);
                                }
                                verbose &= LogExitDebug<TResult>("ParseStatement", result);
                                expression = parser.expression;
                                return result;
                            }
                            catch (Exception e)
                            {
                                Type type = GetRequiredType(e);
                                if (type == null || typeof(T) == typeof(TResult))
                                {
                                    throw;
                                }

                                expression = savedExpression;
                                lval = ParseOperation<T>(lval, token.sval);
                                parser.expression = expression;
                                break;
                            }
                        case TokenType.TERNARY_START:
                            try
                            {
                                // Parse under the return type
                                TResult val = ParseTernary<TResult>(ConvertType<bool>(lval));
                                return val;
                            }
                            catch (Exception e)
                            {
                                Type type = GetRequiredType(e);
                                if (type == null || typeof(T) == typeof(TResult))
                                {
                                    throw;
                                }

                                expression = savedExpression;
                                lval = ParseTernary<T>(ConvertType<bool>(lval));
                                break;
                            }
                        default:
                            expression = token.sval + expression;
                            throw new ArgumentException("Unexpected value: " + token.sval);
                    }

                    // Get next token
                    token = ParseToken();
                }

                result = ConvertType<TResult>(lval);
                verbose &= LogExitDebug<TResult>("ParseStatement", result);
                return result;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseStatement");
                throw;
            }
        }

        public TResult ParseSimpleStatement<TResult>()
        {
            verbose &= LogEntryDebug<TResult>("ParseSimpleStatement");
            try
            {
                // Get a token
                Token token = ParseToken();

                ExpressionParser<TResult> parser = GetParser<TResult>(this);
                bool resetExpression = true;

                try
                {
                    TResult result;
                    switch (token.tokenType)
                    {
                        case TokenType.START_BRACKET:
                            resetExpression = false;
                            result = ParseStatement<TResult>();
                            ParseToken(")");
                            verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                            return result;
                        case TokenType.LIST_START:
                            resetExpression = false;
                            result = ParseList<TResult>();
                            verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                            return result;
                        case TokenType.IDENTIFIER:
                            result = parser.ParseVarOrIdentifier(token);
                            verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                            return result;
                        case TokenType.FUNCTION:
                            result = parser.ParseFunction(token);
                            verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                            return result;
                        case TokenType.SPECIAL_IDENTIFIER:
                            result = parser.ParseSpecialIdentifier(token);
                            verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                            return result;
                        case TokenType.DATA_STORE_IDENTIFIER:
                            result = parser.ParseDataStoreIdentifier(token);
                            verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                            return result;
                        case TokenType.OPERATOR:
                            switch (token.sval)
                            {
                                case "-":
                                    {
                                        resetExpression = false;
                                        TResult value = ParseSimpleStatement<TResult>();
                                        resetExpression = true;
                                        parser.expression = expression;
                                        result = parser.Negate(value);
                                        verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                                        return result;
                                    }
                                case "!":
                                    {
                                        resetExpression = false;
                                        TResult value = ParseSimpleStatement<TResult>();
                                        resetExpression = true;
                                        parser.expression = expression;
                                        result = parser.Not(value);
                                        verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                                        return result;
                                    }
                                default:
                                    resetExpression = false;
                                    expression = token.sval + expression;
                                    throw new ArgumentException("Unexpected operator: " + token.sval);
                            }
                        case TokenType.VALUE:
                            resetExpression = false;
                            result = ConvertType<TResult>((token as ValueToken<T>).val);
                            verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                            return result;
                        case TokenType.QUOTE:
                            if (typeof(TResult) == typeof(string))
                            {
                                parser.expression = token.sval + expression;
                                result = parser.ParseStatement<TResult>();
                                verbose &= LogExitDebug<TResult>("ParseSimpleStatement", result);
                                return result;
                            }
                            else
                            {
                                resetExpression = false;
                                expression = token.sval + expression;
                                throw new DataStoreCastException(typeof(string), typeof(TResult));
                            }
                        default:
                            resetExpression = false;
                            expression = token.sval + expression;
                            throw new ArgumentException("Unexpected value: " + token.sval);
                    }
                }
                finally
                {
                    if (resetExpression)
                    {
                        expression = parser.expression;
                    }
                }
            }
            catch
            {
                verbose &= LogException<TResult>("ParseSimpleStatement");
                throw;
            }
        }

        private TResult ParseOperation<TResult>(T lval, string op)
        {
            verbose &= LogEntryDebug<TResult>("ParseOperation", lval, op);
            try
            {
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
                            verbose &= LogExitDebug<TResult>("ParseOperation", result);
                            return result;
                        case TokenType.TERNARY_START:
                            result = ParseTernary<TResult>(ApplyOperator<bool>(lval, op, rval));
                            verbose &= LogExitDebug<TResult>("ParseOperation", result);
                            return result;
                        case TokenType.OPERATOR:
                            if (precedence[op] >= precedence[token.sval])
                            {
                                expression = token.sval + expression;
                                result = ApplyOperator<TResult>(lval, op, rval);
                                verbose &= LogExitDebug<TResult>("ParseOperation", result);
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
                verbose &= LogExitDebug<TResult>("ParseOperation", result);
                return result;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseOperation");
                throw;
            }
        }

        public virtual TResult ParseList<TResult>()
        {
            verbose &= LogEntryDebug<TResult>("ParseList");
            try
            {
                List<T> values = new List<T>();
                values.Add(ParseStatement<T>());

                Token token = null;
                while (token == null)
                {
                    token = ParseToken();
                    if (token == null)
                    {
                        throw new ArgumentException("Expected ',' or ']', got end of statement.");
                    }

                    switch (token.tokenType)
                    {
                        case TokenType.COMMA:
                            values.Add(ParseStatement<T>());
                            token = null;
                            break;
                        case TokenType.LIST_END:
                            break;
                        default:
                            expression = token.sval + expression;
                            throw new ArgumentException("Unexpected value: " + token.sval);
                    }
                }

                // See what's next to parse
                string savedExpression = expression;
                token = ParseToken();
                ExpressionParser<List<T>> parser = GetParser<List<T>>(this);
                if (token == null)
                {
                    return parser.ConvertType<TResult>(values);
                }
                else if (token.tokenType == TokenType.METHOD)
                {
                    // Parse a method call
                    try
                    {
                        TResult result = parser.ParseMethod<TResult>(token, values);
                        verbose &= LogExitDebug<TResult>("ParseList", result);
                        return result;
                    }
                    finally
                    {
                        expression = parser.expression;
                    }
                }
                else
                {
                    expression = savedExpression;

                    if (typeof(TResult) == typeof(List<T>))
                    {
                        return (TResult)(object)values;
                    }

                    throw new DataStoreCastException(typeof(TResult), typeof(List<T>));
                }
            }
            catch
            {
                verbose &= LogException<TResult>("ParseList");
                throw;
            }
        }

        public T GetRval()
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
                MethodInfo method = altParser.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).
                    Where(m => m.Name == "ParseStatementInner").Single();
                method = method.MakeGenericMethod(new Type[] { typeof(T) });
                try
                {
                    T result = (T)method.Invoke(altParser, new object[] { });
                    return result;
                }
                catch (TargetInvocationException tie)
                {
                    e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                    if (e != null)
                    {
                        throw e;
                    }
                    throw;
                }
                finally
                {
                    expression = altParser.expression;
                }
            }
        }

        public TResult ParseTernary<TResult>(bool lval)
        {
            verbose &= LogEntryDebug<TResult>("ParseTernary", lval);

            ExpressionParser<TResult> parser = GetParser<TResult>(this);
            TResult result;
            try
            {
                TResult val1 = parser.ParseStatement<TResult>();
                parser.ParseToken(":");
                TResult val2 = parser.ParseStatement<TResult>();

                result = lval ? val1 : val2;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseTernary");
                throw;
            }

            expression = parser.expression;
            verbose &= LogExitDebug<TResult>("ParseTernary", result);
            return result;
        }

        public Token ParseToken()
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
                case '[':
                    expression = expression.Substring(1);
                    return new Token(TokenType.LIST_START);
                case ']':
                    expression = expression.Substring(1);
                    return new Token(TokenType.LIST_END);
                case '"':
                    expression = expression.Substring(1);
                    return new Token(TokenType.QUOTE);
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
                case '$':
                    return ParseDataStoreIdentifier();
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

        public Token ParseMethodToken()
        {
            string savedExpression = expression;
            Token token;
            try
            {
                token = ParseToken();
            }
            catch (ArgumentException)
            {
                expression = savedExpression;
                return null;
            }

            if (token == null)
            {
                expression = savedExpression;
                return null;
            }

            if (token.tokenType == TokenType.METHOD)
            {
                return token;
            }

            expression = savedExpression;
            return null;
        }

        public Token ParseMethodEndToken()
        {
            string savedExpression = expression;
            Token token;
            try
            {
                token = ParseToken();
            }
            catch
            {
                expression = savedExpression;
                return null;
            }

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
                expression = savedExpression;
                return null;
            }
        }

        public void ParseToken(string expected)
        {
            Token token = ParseToken();
            if (token == null)
            {
                throw new ArgumentException("Expected '" + expected + "', but reached end of expression!");
            }
            if (token.sval != expected)
            {
                throw new ArgumentException("Expected '" + expected + "', got: " + token.sval);
            }
        }

        public virtual T ParseVarOrIdentifier(Token token)
        {
            // Look it up in temporary variables
            if (tempVariables.ContainsKey(token.sval))
            {
                KeyValuePair<object, Type> pair = tempVariables[token.sval];

                // Check for a method call before we start messing with types
                Token methodToken = ParseMethodToken();
                if (methodToken != null)
                {
                    BaseParser methodParser = GetParser(pair.Value);

                    MethodInfo parseMethod = methodParser.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).
                        Where(m => m.Name == "ParseMethod" && m.GetParameters().Count() == 3).Single();
                    parseMethod = parseMethod.MakeGenericMethod(new Type[] { typeof(T) });

                    try
                    {
                        return (T)parseMethod.Invoke(methodParser, new object[] { methodToken, pair.Key, false });
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                        if (e != null)
                        {
                            throw e;
                        }
                        throw;
                    }
                    finally
                    {
                        expression = methodParser.expression;
                    }
                }

                return ConvertType(pair.Key, pair.Value);
            }

            return ParseIdentifier(token);
        }

        /// <summary>
        /// Parses an identifier for a value stored in the persistant data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the identifier</returns>
        public virtual T ParseIdentifier(Token token)
        {
            throw new NotSupportedException("Can't parse identifier for type " + typeof(T) + " in class " + this.GetType() + " - not supported!");
        }

        public virtual TResult ParseMethod<TResult>(Token token, T obj, bool isFunction = false)
        {
            verbose &= LogEntryDebug<TResult>("ParseMethod", token.sval, obj, isFunction);
            try
            {
                // Start with method call
                ParseToken("(");

                Function selectedMethod = null;
                IEnumerable<object> parameters = GetCalledFunction(token.sval, ref selectedMethod, isFunction);

                // Add object to the parameter list
                if (!isFunction)
                {
                    List<object> newParam = new List<object>();
                    newParam.Add(obj);
                    newParam.AddRange(parameters);
                    parameters = newParam;
                }

                // Invoke the method
                object result;
                try
                {
                    currentParser = this;
                    result = selectedMethod.Invoke(parameters.ToArray());
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                    if (e != null)
                    {
                        throw e;
                    }
                    throw;
                }

                if (!selectedMethod.Deterministic && currentDataNode != null)
                {
                    currentDataNode.SetDeterministic(currentKey, false);
                }

                Type returnType = selectedMethod.ReturnType();

                // Check for a method call before we return
                string savedExpression = expression;
                token = ParseToken();
                if (token != null && token.tokenType == TokenType.METHOD)
                {
                    BaseParser methodParser = GetParser(returnType);

                    MethodInfo parseMethod = methodParser.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).
                        Where(m => m.Name == "ParseMethod" && m.GetParameters().Count() == 3).Single();
                    parseMethod = parseMethod.MakeGenericMethod(new Type[] { typeof(TResult) });
                    returnType = typeof(TResult);

                    try
                    {
                        result = parseMethod.Invoke(methodParser, new object[] { token, result, false });
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                        if (e != null)
                        {
                            throw e;
                        }
                        throw;
                    }
                    finally
                    {
                        expression = methodParser.expression;
                    }
                }
                // Special handling for boolean return
                else if (token != null && token.tokenType == TokenType.OPERATOR && IsBoolean(token.sval) &&
                    typeof(TResult) == typeof(bool) && selectedMethod.ReturnType() != typeof(bool))
                {
                    BaseParser parser = GetParser(selectedMethod.ReturnType());

                    MethodInfo getRval = parser.GetType().GetMethod("GetRval",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    MethodInfo applyBooleanOperator = parser.GetType().GetMethod("ApplyBooleanOperator",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                    try
                    {
                        object rval = getRval.Invoke(parser, new object[] { });
                        result = applyBooleanOperator.Invoke(parser, new object[] { result, token.sval, rval });
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                        if (e != null)
                        {
                            throw e;
                        }
                        throw;
                    }
                    finally
                    {
                        expression = parser.expression;
                    }
                }
                else
                {
                    expression = savedExpression;
                }

                // Return the result
                ExpressionParser<TResult> retValParser = GetParser<TResult>(this);
                TResult retVal = retValParser.ConvertType(result, returnType);
                verbose &= LogExitDebug<TResult>("ParseMethod", retVal);
                return retVal;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseMethod");
                throw;
            }
        }

        public IEnumerable<object> GetCalledFunction(string functionName, ref Function selectedMethod, bool isFunction = false)
        {
            IEnumerable<Function> methods;
            
            if (isFunction)
            {
                methods = GetFunctions(functionName);
            }
            else
            {
                if (classMethods.ContainsKey(functionName))
                {
                    methods = classMethods[functionName].ToList();
                }
                else
                {
                    methods = Enumerable.Empty<Function>();
                }
            }

            if (!methods.Any())
            {
                throw new MissingMethodException("Cannot find " + (isFunction ? "function" : "method") + " '" + functionName + "' for class '" + typeof(T).Name + "'.");
            }

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

            List<KeyValuePair<object, Type>> parameters = new List<KeyValuePair<object, Type>>();

            while (true)
            {
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
                                    if (parameters[j].Value != method.ParameterType(j))
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
                        throw new ArgumentException("Expected ')', got: " + (endToken != null ? endToken.sval : "end of statement"));
                    }
                }
                else if (parameters.Count() != 0)
                {
                    Token token = ParseToken();
                    throw new ArgumentException("Expected ',' or ')', got: " + (token != null ? token.sval : "end of statement"));
                }

                // Check for end of statement
                if (expression.Trim() == "")
                {
                    throw new ArgumentException("Expected an expression, got end of statement");
                }

                if (parameters.Count >= paramTypes.Count)
                {
                    throw new ArgumentException("Couldn't find matching signature for " + (isFunction ? "function" : "method") + " '" + functionName + "'.");
                }

                Type paramType = paramTypes.ElementAtOrDefault(parameters.Count);
                // Easy - we have the type!
                if (paramType != null)
                {
                    BaseParser parser = GetParser(paramType);

                    try
                    {
                        MethodInfo method = parser.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).
                            Where(m => m.Name == "ParseStatement" && m.GetParameters().Count() == 0).Single();
                        method = method.MakeGenericMethod(new Type[] { paramType });
                        object value = method.Invoke(parser, new object[] { });
                        parameters.Add(new KeyValuePair<object, Type>(value, paramType));
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                        if (e != null)
                        {
                            throw e;
                        }
                        throw;
                    }
                    finally
                    {
                        expression = parser.expression;
                    }
                }
                else
                {
                    // TODO - implement once there's a use case for more complex overloading
                    throw new NotImplementedException("Something I didn't expect happened!  Raise a GitHub issue!");
                }
            }

            return parameters.Select<KeyValuePair<object, Type>, object>(x => x.Key);
        }

        public T ParseFunction(Token token)
        {
            return ParseMethod<T>(token, default(T), true);
        }

        /// <summary>
        /// Parses an identifier for a config node value.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the config node identifier</returns>
        public virtual T ParseSpecialIdentifier(Token token)
        {
            verbose &= LogEntryDebug<T>("ParseSpecialIdentifier", token);
            try
            {
                if (currentDataNode != null)
                {
                    string identifier = token.sval;
                    DataNode dataNode = currentDataNode;
                    while (identifier.Contains("/"))
                    {
                        if (identifier[0] == '/')
                        {
                            identifier = identifier.Substring(1);
                            dataNode = dataNode.Root ?? dataNode;
                            continue;
                        }
                        else if (identifier.StartsWith("../"))
                        {
                            identifier = identifier.Substring(3);
                            dataNode = dataNode.Parent;
                            continue;
                        }
                        else
                        {
                            int index = identifier.IndexOf('/');
                            string currentIdentifier = identifier.Substring(0, index);
                            identifier = identifier.Substring(index + 1);
                            DataNode newNode = dataNode.Children.Where(dn => dn.Name == currentIdentifier).FirstOrDefault();

                            if (newNode == null)
                            {
                                throw new DataNode.ValueNotInitialized(dataNode.Path() + currentIdentifier + "/" + identifier);
                            }
                            dataNode = newNode;
                        }
                    }

                    // Check if the identifier is a data node (versus a key in the current data node)
                    DataNode childNode = dataNode.Children.Where(dn => dn.Name == identifier).FirstOrDefault();
                    object o = null;
                    Type dataType = null;
                    if (childNode != null)
                    {
                        dataNode = childNode;
                        o = dataNode.Factory;
                        dataType = o.GetType();
                    }
                    // Handle as a simple data value
                    else
                    {
                        if (!dataNode.IsInitialized(identifier))
                        {
                            throw new DataNode.ValueNotInitialized(dataNode.Path() + identifier);
                        }

                        o = dataNode[identifier];
                        if (!dataNode.IsDeterministic(identifier))
                        {
                            currentDataNode.SetDeterministic(currentKey, false);
                        }

                        dataType = dataNode.GetType(identifier);
                    }

                    T result;
                    try
                    {
                        MethodInfo completeIdentifierParsing = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).
                            Where(mi => mi.Name == "CompleteIdentifierParsing").First();
                        completeIdentifierParsing = completeIdentifierParsing.MakeGenericMethod(new Type[] { dataType });

                        result = (T)completeIdentifierParsing.Invoke(this, new object[] { o });
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                        if (e != null)
                        {
                            throw e;
                        }
                        throw;
                    }

                    verbose &= LogExitDebug<T>("ParseSpecialIdentifier", result);
                    return result;
                }
                else
                {
                    throw new ArgumentException("Cannot get value for @" + token.sval + ": not available in this context.");
                }
            }
            catch
            {
                verbose &= LogException<T>("ParseSpecialIdentifier");
                throw;
            }
        }

        public virtual T CompleteIdentifierParsing<U>(U value)
        {
            verbose &= LogEntryDebug<T>("CompleteIdentifierParsing", value);

            try
            {
                // Check for a method call before we start messing with types
                Token methodToken = ParseMethodToken();
                if (methodToken != null)
                {
                    ExpressionParser<U> methodParser = GetParser<U>(this);

                    try
                    {
                        T res = methodParser.ParseMethod<T>(methodToken, value);
                        verbose &= LogExitDebug<T>("CompleteIdentifierParsing", res);
                        return res;
                    }
                    finally
                    {
                        expression = methodParser.expression;
                    }
                }

                // No method, try type conversion or straight return
                T result;
                if (typeof(U) == typeof(T))
                {
                    result = (T)(object)value;
                }
                else
                {
                    result = ConvertType(value, typeof(U));
                }

                verbose &= LogExitDebug<T>("CompleteIdentifierParsing", result);
                return result;
            }
            catch
            {
                verbose &= LogException<T>("CompleteIdentifierParsing");
                throw;
            }
        }

        /// <summary>
        /// Parses an identifier for a value stored in the persistent data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the config node identifier</returns>
        public virtual T ParseDataStoreIdentifier(Token token)
        {
            verbose &= LogEntryDebug<T>("ParseDataStoreIdentifier", token);

            T result;
            try
            {
                // Stored values are always non-deterministic
                currentDataNode.SetDeterministic(currentKey, false);

                object o;
                Type dataType;
                if (PersistentDataStore.Instance != null && PersistentDataStore.Instance.HasKey(token.sval))
                {
                    o = PersistentDataStore.Instance.Retrieve(token.sval, out dataType);
                }
                else
                {
                    o = default(T);
                    dataType = typeof(T);
                }

                try
                {
                    MethodInfo completeIdentifierParsing = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).
                        Where(mi => mi.Name == "CompleteIdentifierParsing").First();
                    completeIdentifierParsing = completeIdentifierParsing.MakeGenericMethod(new Type[] { dataType });

                    result = (T)completeIdentifierParsing.Invoke(this, new object[] { o });
                }
                catch (TargetInvocationException tie)
                {
                    Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                    if (e != null)
                    {
                        throw e;
                    }
                    throw;
                }
            }
            catch
            {
                verbose &= LogException<T>("ParseDataStoreIdentifier");
                throw;
            }

            verbose &= LogExitDebug<T>("ParseDataStoreIdentifier", result);
            return result;
        }

        public Token ParseIdentifier()
        {
            Match m = Regex.Match(expression, @"([A-Za-z][\w\d]*).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length ? expression.Substring(identifier.Length) : "");

            expression.Trim();
            TokenType type = expression.Length > 0 && expression.Substring(0, 1) == "(" ? TokenType.FUNCTION : TokenType.IDENTIFIER;

            return new Token(type, identifier);
        }

        public Token ParseSpecialIdentifier()
        {
            Match m = Regex.Match(expression, @"^@(/?(?>([A-Za-z][\w\d]*|\.\.)/)*[A-Za-z][\w\d:]*).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length + 1 ? expression.Substring(identifier.Length + 1) : "");

            return new Token(TokenType.SPECIAL_IDENTIFIER, identifier);
        }

        public Token ParseDataStoreIdentifier()
        {
            Match m = Regex.Match(expression, @"^\$(/?(?>([A-Za-z][\w\d]*|\.\.)/)*[A-Za-z][\w\d:]*).*");
            string identifier = m.Groups[1].Value;
            expression = (expression.Length > identifier.Length + 1 ? expression.Substring(identifier.Length + 1) : "");

            return new Token(TokenType.DATA_STORE_IDENTIFIER, identifier);
        }

        public Token ParseMethod()
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


        public virtual Token ParseNumericConstant()
        {
            throw new WrongDataType(typeof(double), typeof(T));
        }

        public Token ParseOperator()
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

        public Token ParseOperator(string op)
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

        public TResult ApplyOperator<TResult>(T lval, string op, T rval)
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

        public bool ApplyBooleanOperator(T lval, string op, T rval)
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
        /// Returns true if a reverse conversion can be done for the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual bool ConvertableFrom(Type type)
        {
            return false;
        }

        /// <summary>
        /// Performs a reverse data type conversion.
        /// </summary>
        /// <typeparam name="U">Type to convert from</typeparam>
        /// <param name="value">Value to convert</param>
        /// <returns>The converted value</returns>
        public virtual T ConvertFrom<U>(U value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a data type conversion.
        /// </summary>
        /// <typeparam name="U">Type to convert to.</typeparam>
        /// <param name="value">Value to convert from.</param>
        /// <returns>The converted value.</returns>
        public virtual U ConvertType<U>(T value)
        {
            Type tType = typeof(T);
            Type uType = typeof(U);

            // Handle the basic case
            if (tType == uType)
            {
                return (U)(object)value;
            }

            // Special string handling
            if (uType == typeof(string))
            {
                if (value == null)
                {
                    return (U)(object)"";
                }

                return (U)(object)value.ToString();
            }

            // Disallow conversion directly to a boolean
            if (uType == typeof(bool) || tType == typeof(bool))
            {
                // Exception - strings
                if (tType == typeof(string) && "False".Equals(value) || "True".Equals(value))
                {
                    return (U)Convert.ChangeType(value, uType);
                }

                throw new DataStoreCastException(tType, typeof(U));
            }

            // Do a reverse conversion
            ExpressionParser<U> parser = GetParser<U>(this);
            if (parser != null && parser.ConvertableFrom(typeof(T)))
            {
                return (U)parser.ConvertFrom<T>(value);
            }

            // Try basic conversion
            try
            {
                return (U)Convert.ChangeType(value, uType);
            }
            catch
            {
                throw new DataStoreCastException(tType, uType);
            }
        }

        public T ConvertType(object value)
        {
            // Handle null input values
            if (value == null)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)"";
                }
                throw new ArgumentNullException();
            }

            return ConvertType(value, value.GetType());
        }


        public T ConvertType(object value, Type type)
        {
            if (value == null)
            {
                if (type == typeof(T))
                {
                    try
                    {
                        return (T)value;
                    }
                    catch
                    {
                        throw new DataStoreCastException(type, typeof(T));
                    }
                }
                else
                {
                    // Special case
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)"";
                    }

                    throw new DataStoreCastException(type, typeof(T));
                }
            }
            else if (typeof(T).IsAssignableFrom(type))
            {
                return (T)(object)value;
            }

            MethodInfo convertMethod = GetType().GetMethod("_ConvertType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            convertMethod = convertMethod.MakeGenericMethod(new Type[] { value.GetType() });

            try
            {
                T result = (T)convertMethod.Invoke(this, new object[] { value });
                return result;
            }
            catch (TargetInvocationException tie)
            {
                Exception e = ExceptionUtil.UnwrapTargetInvokationException(tie);
                if (e != null)
                {
                    throw e;
                }
                throw;
            }
        }

        public T _ConvertType<U>(U value)
        {
            if (typeof(T).IsAssignableFrom(typeof(U)))
            {
                return (T)(object)value;
            }
            else
            {
                try
                {
                    ExpressionParser<U> parser = GetParser<U>(this);
                    return parser.ConvertType<T>(value);
                }
                catch (NotSupportedException)
                {
                    if (typeof(U) != typeof(object))
                    {
                        MethodInfo convertMethod = GetType().GetMethod("_ConvertType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                        convertMethod = convertMethod.MakeGenericMethod(new Type[] { typeof(U).BaseType });
                        T result = (T)convertMethod.Invoke(this, new object[] { value });
                        return result;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public bool IsBoolean(string op)
        {
            string[] booleans = { "!", "||", "&&", "<", "<=", "==", "!=", ">", ">=" };
            return booleans.Contains(op);
        }

        public virtual T Negate(T val)
        {
            throw new NotSupportedException("Negation (-) not supported for type " + typeof(T));
        }

        public virtual T Add(T a, T b)
        {
            throw new NotSupportedException("Addition (+) not supported for type " + typeof(T));
        }

        public virtual T Sub(T a, T b)
        {
            throw new NotSupportedException("Subtraction (-) not supported for type " + typeof(T));
        }

        public virtual T Mult(T a, T b)
        {
            throw new NotSupportedException("Multiplication (*) not supported for type " + typeof(T));
        }

        public virtual T Div(T a, T b)
        {
            throw new NotSupportedException("Division (/) not supported for type " + typeof(T));
        }

        public virtual T Not(T val)
        {
            throw new NotSupportedException("Logical NOT (!) is not supported for type " + typeof(T));
        }

        public virtual bool Or(T a, T b)
        {
            throw new NotSupportedException("Logical OR (||) not supported for type " + typeof(T));
        }

        public virtual bool And(T a, T b)
        {
            throw new NotSupportedException("Logical AND (&&) not supported for type " + typeof(T));
        }

        public virtual bool LT(T a, T b)
        {
            throw new NotSupportedException("Less than (<) not supported for type " + typeof(T));
        }

        public virtual bool LE(T a, T b)
        {
            throw new NotSupportedException("Less than or equal (<=) not supported for type " + typeof(T));
        }

        public virtual bool EQ(T a, T b)
        {
            throw new NotSupportedException("Equal (==) not supported for type " + typeof(T));
        }

        public virtual bool NE(T a, T b)
        {
            throw new NotSupportedException("Equal (==) not supported for type " + typeof(T));
        }

        public virtual bool GE(T a, T b)
        {
            throw new NotSupportedException("Greater than or equal (>=) not supported for type " + typeof(T));
        }

        public virtual bool GT(T a, T b)
        {
            throw new NotSupportedException("Greater than (>) not supported for type " + typeof(T));
        }
    }
}
