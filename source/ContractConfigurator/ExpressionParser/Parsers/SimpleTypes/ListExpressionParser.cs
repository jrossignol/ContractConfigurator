using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Special expression parser subclass for Lists.  Automatically registered for every type registered.
    /// </summary>
    public class ListExpressionParser<T> : ClassExpressionParser<List<T>>
    {
        static Random r = new Random();

        static ListExpressionParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(CelestialBody), typeof(CelestialBodyParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<List<T>, T>("Random", l => l.Any() ? l.Skip(r.Next(l.Count)).First() : default(T), false));
            RegisterMethod(new Method<List<T>, int, List<T>>("Random", RandomList, false));
            RegisterMethod(new Method<List<T>, T>("First", l => l.FirstOrDefault()));
            RegisterMethod(new Method<List<T>, T>("Last", l => l.LastOrDefault()));
            RegisterMethod(new Method<List<T>, int, T>("ElementAt", (l, i) => l.ElementAtOrDefault(i)));

            RegisterMethod(new Method<List<T>, T, bool>("Contains", (l, o) => l.Contains(o)));

            RegisterMethod(new Method<List<T>, int>("Count", l => l.Count));

            RegisterMethod(new Method<List<T>, List<T>, List<T>>("Concat", (l1, l2) => { l1.ToList().AddRange(l2); return l1; }));
            RegisterMethod(new Method<List<T>, T, List<T>>("Add", (l, v) => { l.ToList().Add(v); return l; }));
            RegisterMethod(new Method<List<T>, T, List<T>>("Exclude", (l, v) => { l = l.ToList(); l.Remove(v); return l; }));
            RegisterMethod(new Method<List<T>, List<T>, List<T>>("ExcludeAll", (l, l2) => { l = l.ToList(); l.RemoveAll(x => l2.Contains(x)); return l; }));
        }

        protected static List<T> RandomList(List<T> input, int count)
        {
            if (count >= input.Count())
            {
                return input;
            }

            List<T> output = new List<T>();
            int remaining = count;
            int size = input.Count();
            foreach (T value in input)
            {
                double p = (double)remaining / size--;
                if (r.NextDouble() < p)
                {
                    remaining--;
                    output.Add(value);

                    if (output.Count == count)
                    {
                        break;
                    }
                }
            }

            return output;
        }

        public ListExpressionParser()
        {
        }

        internal override TResult ParseMethod<TResult>(Token token, List<T> obj, bool isFunction = false)
        {
            if (token.sval == "Where")
            {
                return ParseWhereMethod<TResult>(obj);
            }
            else
            {
                return base.ParseMethod<TResult>(token, obj, isFunction);
            }
        }

        internal TResult ParseWhereMethod<TResult>(List<T> obj)
        {
            verbose &= LogEntryDebug<TResult>("ParseWhereMethod", obj != null ? obj.ToString() : "null");
            try
            {
                // Start with method call
                ParseToken("(");

                // Get the identifier for the object
                Match m = Regex.Match(expression, @"([A-Za-z][\w\d]*)[\s]*=>[\s]*(.*)");
                string identifier = m.Groups[1].Value;
                expression = (string.IsNullOrEmpty(identifier) ? expression : m.Groups[2].Value);

                List<T> values = obj.Count == 0 ? new T[] { default(T) }.ToList() : obj;
                List<T> filteredList = new List<T>();

                // Save the expression, then execute for each value
                string savedExpression = expression;
                try
                {
                    foreach (T value in values)
                    {
                        expression = savedExpression;
                        tempVariables[identifier] = new KeyValuePair<object, Type>(value, typeof(T));
                        ExpressionParser<T> parser = GetParser<T>(this);
                        try
                        {
                            bool keep = parser.ParseStatement<bool>();
                            if (keep && obj.Count != 0)
                            {
                                filteredList.Add(value);
                            }
                        }
                        finally
                        {
                            expression = parser.expression;
                        }
                    }
                }
                finally
                {
                    if (tempVariables.ContainsKey(identifier))
                    {
                        tempVariables.Remove(identifier);
                    }
                }

                // Finish the method call
                ParseToken(")");

                // Check for a method call before we return
                Token methodToken = ParseMethodToken();
                ExpressionParser<TResult> retValParser = GetParser<TResult>(this);
                TResult result;
                if (methodToken != null)
                {
                    result = ParseMethod<TResult>(methodToken, filteredList);
                }
                else
                {
                    // No method, attempt to convert - most likely fails
                    result = retValParser.ConvertType(filteredList);
                }

                verbose &= LogExitDebug<TResult>("ParseWhereMethod", result);
                return result;
            }
            catch
            {
                verbose &= LogException<TResult>("ParseWhereMethod");
                throw;
            }
        }
        
        internal override TResult ParseList<TResult>()
        {
            // Use the regular type parser to do the parting
            ExpressionParser<T> parser = GetParser<T>(this);
            try
            {
                return parser.ParseList<TResult>();
            }
            finally
            {
                expression = parser.expression;
            }
        }
    }
}
