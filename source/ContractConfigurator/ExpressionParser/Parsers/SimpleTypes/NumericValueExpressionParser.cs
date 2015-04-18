using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator.ExpressionParser
{
    public class ValueParserRegistrer : IExpressionParserRegistrer
    {
        public void RegisterExpressionParsers()
        {
            BaseParser.RegisterParserType(typeof(uint), typeof(NumericValueExpressionParser<uint>));
            BaseParser.RegisterParserType(typeof(int), typeof(NumericValueExpressionParser<int>));
            BaseParser.RegisterParserType(typeof(float), typeof(NumericValueExpressionParser<float>));
            BaseParser.RegisterParserType(typeof(double), typeof(NumericValueExpressionParser<double>));
        }
    }

    public abstract class Calculator<T>
    {
        public abstract T Negate(T val);
        public abstract T Add(T a, T b);
        public abstract T Sub(T a, T b);
        public abstract T Mult(T a, T b);
        public abstract T Div(T a, T b);
        public abstract bool EQ(T a, T b);
        public abstract bool NE(T a, T b);
    }

    public class IntCalculator : Calculator<int>
    {
        public override int Negate(int val) { return -val; }
        public override int Add(int a, int b) { return a + b; }
        public override int Sub(int a, int b) { return a - b; }
        public override int Mult(int a, int b) { return a * b; }
        public override int Div(int a, int b) { return a / b; }
        public override bool EQ(int a, int b) { return a == b; }
        public override bool NE(int a, int b) { return a != b; }
    }

    public class UIntCalculator : Calculator<uint>
    {
        public override uint Negate(uint val)
        {
            throw new NotSupportedException("Negation (-) not supported for type " + typeof(uint));
        }
        public override uint Add(uint a, uint b) { return a + b; }
        public override uint Sub(uint a, uint b) { return a - b; }
        public override uint Mult(uint a, uint b) { return a * b; }
        public override uint Div(uint a, uint b) { return a / b; }
        public override bool EQ(uint a, uint b) { return a == b; }
        public override bool NE(uint a, uint b) { return a != b; }
    }

    public class FloatCalculator : Calculator<float>
    {
        public override float Negate(float val) { return -val; }
        public override float Add(float a, float b) { return a + b; }
        public override float Sub(float a, float b) { return a - b; }
        public override float Mult(float a, float b) { return a * b; }
        public override float Div(float a, float b) { return a / b; }
        public override bool EQ(float a, float b) { return Math.Abs(a - b) <= 0.001; }
        public override bool NE(float a, float b) { return Math.Abs(a - b) > 0.001; }
    }

    public class DoubleCalculator : Calculator<double>
    {
        public override double Negate(double val) { return -val; }
        public override double Add(double a, double b) { return a + b; }
        public override double Sub(double a, double b) { return a - b; }
        public override double Mult(double a, double b) { return a * b; }
        public override double Div(double a, double b) { return a / b; }
        public override bool EQ(double a, double b) { return Math.Abs(a - b) <= 0.001; }
        public override bool NE(double a, double b) { return Math.Abs(a - b) > 0.001; }
    }

    public class NumericValueExpressionParser<T> : ComparableValueExpressionParser<T> where T : struct, IComparable<T>
    {
        private static Calculator<T> calculator;
        private static System.Random random = new System.Random();

        static NumericValueExpressionParser()
        {
            RegisterMethods();
        }
    
        internal static void RegisterMethods()
        {
            if (typeof(T) == typeof(int))
            {
                calculator = new IntCalculator() as Calculator<T>;
            }
            else if (typeof(T) == typeof(uint))
            {
                calculator = new UIntCalculator() as Calculator<T>;
            }
            else if (typeof(T) == typeof(float))
            {
                calculator = new FloatCalculator() as Calculator<T>;
            }
            else if (typeof(T) == typeof(double))
            {
                calculator = new DoubleCalculator() as Calculator<T>;
            }

            RegisterLocalFunction(new Function<T>("Random", () => (T)Convert.ChangeType(random.NextDouble(), typeof(T)), false));
            RegisterLocalFunction(new Function<T, T, T>("Random", RandomMinMax, false));
            RegisterLocalFunction(new Function<T, T, T>("Max", Max));
            RegisterLocalFunction(new Function<T, T, T>("Min", Min));

            RegisterMethod(new Method<T, string>("Print", (tval) =>
            {
                if (typeof(T) == typeof(int) || typeof(T) == typeof(short))
                {
                    int ival = (int)(object)tval;
                    if (ival == 0) return "zero";
                    if (ival == 1) return "one";
                    if (ival == 2) return "two";
                    if (ival == 3) return "three";
                    if (ival == 4) return "four";
                    if (ival == 5) return "five";
                    if (ival == 6) return "six";
                    if (ival == 7) return "seven";
                    if (ival == 8) return "eight";
                    if (ival == 9) return "nine";
                    return ival.ToString("N0");
                }
                else if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    double dval = (double)(object)tval;
                    if (dval < 1.0)
                    {
                        return dval.ToString("N5");
                    }
                    return dval.ToString("N2");
                }
                return tval.ToString();
            }));
        }

        private static T RandomMinMax(T min, T max)
        {
            double dmin = (double)Convert.ChangeType(min, typeof(double));
            double dmax = (double)Convert.ChangeType(max, typeof(double));

            double val = random.NextDouble() * (dmax - dmin) + dmin;
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Min(T a, T b)
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        private static T Max(T a, T b)
        {
            return a.CompareTo(b) > 0 ? a : b;
        }

        public NumericValueExpressionParser()
            : base()
        {
            if (calculator == null)
            {
                throw new NotSupportedException("Type " + typeof(T) + " is not supported!");
            }
        }

        internal override Token ParseNumericConstant()
        {
            int index = expression.IndexOfAny(WHITESPACE_OR_OPERATOR, 0);

            try
            {
                T val;
                if (index >= 0)
                {
                    val = (T)Convert.ChangeType(expression.Substring(0, index), typeof(T));
                    expression = (expression.Length > index ? expression.Substring(index) : "");
                }
                else
                {
                    val = (T)Convert.ChangeType(expression, typeof(T));
                    expression = "";
                }
                return new ValueToken<T>(val);
            }
            catch (Exception e)
            {
                throw new NotSupportedException("Couldn't parse numeric constant!", e);
            }
        }

        internal override T Negate(T val)
        {
            return calculator.Negate(val);
        }

        internal override T Add(T a, T b)
        {
            return calculator.Add(a, b);
        }

        internal override T Sub(T a, T b)
        {
            return calculator.Sub(a, b);
        }

        internal override T Mult(T a, T b)
        {
            return calculator.Mult(a, b);
        }

        internal override T Div(T a, T b)
        {
            return calculator.Div(a, b);
        }

        internal override bool EQ(T a, T b)
        {
            return calculator.EQ(a, b);
        }

        internal override bool NE(T a, T b)
        {
            return calculator.NE(a, b);
        }

        public void ExecuteAndStoreExpression(string key, string expression, DataNode dataNode)
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

        /// <summary>
        /// Parses an identifier for a value stored in the persistant data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the identifier</returns>
        internal override T ParseIdentifier(Token token)
        {
            if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
            {
                if (parseMode)
                {
                    if (currentDataNode != null)
                    {
                        currentDataNode.SetDeterministic(currentKey, false);
                    }

                    return default(T);
                }
                else if (PersistentDataStore.Instance != null)
                {
                    double dval = PersistentDataStore.Instance.Retrieve<double>(token.sval);
                    return (T)Convert.ChangeType(dval, typeof(T));
                }
                else
                {
                    LoggingUtil.LogWarning(this, "Unable to retrieve value for '" + token.sval + "' - PersistentDataStore is null.  This is likely caused by another ScenarioModule crashing, preventing others from loading.");
                    return default(T);
                }
            }
            else
            {
                return base.ParseIdentifier(token);
            }
        }
    }
}
