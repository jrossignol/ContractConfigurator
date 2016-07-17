using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Contracts;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Special expression parser subclass for Lists.  Automatically registered for every type registered.
    /// </summary>
    public class ListExpressionParser<T> : ClassExpressionParser<List<T>>
    {
        public override MethodInfo methodParseMethod { get { return _methodParseMethod; } }
        static MethodInfo _methodParseMethod = typeof(ListExpressionParser<T>).GetMethods(BindingFlags.Public | BindingFlags.Instance).
            Where(m => m.Name == "ParseMethod" && m.GetParameters().Count() == 3).Single();

        static Random r = new Random();

        static ListExpressionParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(CelestialBody), typeof(CelestialBodyParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<List<T>, T>("Random", l => l == null || !l.Any() ? default(T) : l.Skip(r.Next(l.Count)).First(), false));
            RegisterMethod(new Method<List<T>, int, List<T>>("Random", RandomList, false));
            RegisterMethod(new Method<List<T>, int, int, List<T>>("Random", RandomList, false));
            RegisterMethod(new Method<List<T>, T>("First", l => l == null ? default(T) : l.FirstOrDefault()));
            RegisterMethod(new Method<List<T>, T>("Last", l => l == null ? default(T) : l.LastOrDefault()));
            RegisterMethod(new Method<List<T>, int, T>("ElementAt", (l, i) => l == null ? default(T) : l.ElementAtOrDefault(i)));

            RegisterMethod(new Method<List<T>, T, bool>("Contains", (l, o) => l == null ? false : l.Contains(o)));

            RegisterMethod(new Method<List<T>, int>("Count", l => l == null ? 0 : l.Count));

            RegisterMethod(new Method<List<T>, List<T>, List<T>>("Concat", Concat));
            RegisterMethod(new Method<List<T>, T, List<T>>("Add", (l, v) => { if (l == null) { l = new List<T>(); } l.ToList().Add(v); return l; }));
            RegisterMethod(new Method<List<T>, T, List<T>>("Exclude", (l, v) => { if (l != null) { l = l.ToList(); l.Remove(v); }  return l; }));
            RegisterMethod(new Method<List<T>, List<T>, List<T>>("ExcludeAll", (l, l2) => { if (l != null) { l = l.ToList(); if (l2 != null) { l.RemoveAll(x => l2.Contains(x)); } } return l; }));

            RegisterMethod(new Method<List<T>, T>("SelectUnique", SelectUnique, false));
        }

        protected static List<T> RandomList(List<T> input, int minCount, int maxCount)
        {
            return RandomList(input, NumericValueExpressionParser<int>.RandomMinMax(minCount, maxCount));
        }

        protected static List<T> RandomList(List<T> input, int count)
        {
            if (count >= input.Count())
            {
                List<T> newList = input.ToList();
                newList.Shuffle();
                return newList;
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

        protected static List<T> Concat(List<T> l1, List<T> l2)
        {
            if (l1 == null && l2 == null)
            {
                return new List<T>();
            }
            else if (l1 == null)
            {
                return l2.ToList();
            }
            else if (l2 == null)
            {
                return l1.ToList();
            }

            List<T> newList = l1.ToList();
            newList.AddRange(l2);
            return newList;
        }

        protected static T SelectUnique(List<T> input)
        {
            // Check if there's no values
            if (input == null || !input.Any())
            {
                return default(T);
            }

            // Get details from the base parser
            ContractType contractType = BaseParser.currentParser.currentDataNode.Root.Factory as ContractType;
            string key = BaseParser.currentParser.currentKey;
            DataNode.UniquenessCheck uniquenessCheck = contractType.uniquenessChecks.ContainsKey(key) ? contractType.uniquenessChecks[key] : DataNode.UniquenessCheck.NONE;
            DataNode dataNode = BaseParser.currentParser.currentDataNode;

            // Provide warning of a better method
            if (dataNode != null && dataNode.IsDeterministic(key) && (uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ALL || uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ACTIVE))
            {
                IContractConfiguratorFactory factory = BaseParser.currentParser.currentDataNode.Factory;
                LoggingUtil.LogWarning(factory, factory.ErrorPrefix() + ": Consider using a DATA_EXPAND node instead of the SelectUnique function when the values are deterministic - this will cause the player to see the full set of values in mission control before the contract is offered.");
            }

            // Check for properly uniquness check
            if (uniquenessCheck == DataNode.UniquenessCheck.NONE)
            {
                throw new NotSupportedException("The SelectUnique method can only be used in DATA nodes with the uniquenessCheck attribute set.");
            }

            // Get the active/offered contract lists
            IEnumerable<ConfiguredContract> contractList = ConfiguredContract.CurrentContracts.
                Where(c => c != null && c.contractType != null);

            // Add in finished contracts
            if (uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ALL || uniquenessCheck == DataNode.UniquenessCheck.GROUP_ALL)
            {
                contractList = contractList.Union(ConfiguredContract.CompletedContracts.
                    Where(c => c != null && c.contractType != null));
            }

            // Filter anything that doesn't have our key
            contractList = contractList.Where(c => c.uniqueData.ContainsKey(key));

            // Check for contracts of the same type
            if (uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ALL || uniquenessCheck == DataNode.UniquenessCheck.CONTRACT_ACTIVE)
            {
                contractList = contractList.Where(c => c.contractType.name == contractType.name);
            }
            // Check for a shared group
            else if (contractType.group != null)
            {
                contractList = contractList.Where(c => c.contractType.group != null && c.contractType.group.name == contractType.group.name);
            }
            // Shared lack of group
            else
            {
                contractList = contractList.Where(c => c.contractType.group == null);
            }

            // Get the valid values
            IEnumerable<T> values;
            // Special case for vessels
            if (typeof(T) == typeof(Vessel))
            {
                values = input.Where(t => !contractList.Any(c => c.uniqueData[key].Equals(((Vessel)(object)t).id)));
            }
            else
            {
                values = input.Where(t => !contractList.Any(c => c.uniqueData[key].Equals(t)));
            }

            // Make a random selection from what's left
            return values.Skip(r.Next(values.Count())).FirstOrDefault();
        }


        public ListExpressionParser()
        {
        }

        public override TResult ParseMethod<TResult>(Token token, List<T> obj, bool isFunction = false)
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

        public TResult ParseWhereMethod<TResult>(List<T> obj)
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

                List<T> values = obj == null || obj.Count == 0 ? new T[] { default(T) }.ToList() : obj;
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
        
        public override TResult ParseList<TResult>()
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

        public override U ConvertType<U>(List<T> value)
        {
            ExpressionParser<T> parserT = GetParser<T>();

            if (typeof(U) == typeof(string))
            {
                string result = "[ ";
                foreach (T t in value)
                {
                    result += parserT.ConvertType<string>(t);
                    result += ",";
                }
                result = result.TrimEnd(new char[] { ',' }) + " ]";
                return (U)(object)result;
            }

            return base.ConvertType<U>(value);
        }
    }
}
