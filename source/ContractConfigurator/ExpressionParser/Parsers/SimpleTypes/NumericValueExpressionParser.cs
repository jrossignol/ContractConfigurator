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
            BaseParser.RegisterParserType(typeof(int), typeof(NumericValueExpressionParser<int>));
            BaseParser.RegisterParserType(typeof(long), typeof(NumericValueExpressionParser<long>));
            BaseParser.RegisterParserType(typeof(uint), typeof(NumericValueExpressionParser<uint>));
            BaseParser.RegisterParserType(typeof(ulong), typeof(NumericValueExpressionParser<ulong>));
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

    public class LongCalculator : Calculator<long>
    {
        public override long Negate(long val) { return -val; }
        public override long Add(long a, long b) { return a + b; }
        public override long Sub(long a, long b) { return a - b; }
        public override long Mult(long a, long b) { return a * b; }
        public override long Div(long a, long b) { return a / b; }
        public override bool EQ(long a, long b) { return a == b; }
        public override bool NE(long a, long b) { return a != b; }
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

    public class ULongCalculator : Calculator<ulong>
    {
        public override ulong Negate(ulong val)
        {
            throw new NotSupportedException("Negation (-) not supported for type " + typeof(ulong));
        }
        public override ulong Add(ulong a, ulong b) { return a + b; }
        public override ulong Sub(ulong a, ulong b) { return a - b; }
        public override ulong Mult(ulong a, ulong b) { return a * b; }
        public override ulong Div(ulong a, ulong b) { return a / b; }
        public override bool EQ(ulong a, ulong b) { return a == b; }
        public override bool NE(ulong a, ulong b) { return a != b; }
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
    
        public static void RegisterMethods()
        {
            if (typeof(T) == typeof(int))
            {
                calculator = new IntCalculator() as Calculator<T>;
                RegisterGlobalFunction(new Function<int, int>("int", val => val));
                RegisterGlobalFunction(new Function<int>("IteratorCurrentIndex", () => DataNode.IteratorCurrentIndex, false));
            }
            else if (typeof(T) == typeof(long))
            {
                calculator = new LongCalculator() as Calculator<T>;
                RegisterGlobalFunction(new Function<long, long>("long", val => val));
            }
            else if (typeof(T) == typeof(uint))
            {
                calculator = new UIntCalculator() as Calculator<T>;
                RegisterGlobalFunction(new Function<uint, uint>("uint", val => val));
            }
            else if (typeof(T) == typeof(ulong))
            {
                calculator = new ULongCalculator() as Calculator<T>;
                RegisterGlobalFunction(new Function<ulong, ulong>("ulong", val => val));
            }
            else if (typeof(T) == typeof(float))
            {
                calculator = new FloatCalculator() as Calculator<T>;
                RegisterGlobalFunction(new Function<float, float>("float", val => val));

                RegisterGlobalFunction(new Function<float>("Reputation", () => Reputation.Instance != null ? Reputation.Instance.reputation : 0.0f, false));
                RegisterGlobalFunction(new Function<float>("StartingReputation", () => HighLogic.CurrentGame != null ? HighLogic.CurrentGame.Parameters.Career.StartingReputation : 0.0f, false));
                RegisterGlobalFunction(new Function<float>("Science", () => ResearchAndDevelopment.Instance != null ? ResearchAndDevelopment.Instance.Science : 0.0f, false));
                RegisterGlobalFunction(new Function<float>("StartingScience", () => HighLogic.CurrentGame != null ? HighLogic.CurrentGame.Parameters.Career.StartingScience : 0.0f, false));
            }
            else if (typeof(T) == typeof(double))
            {
                calculator = new DoubleCalculator() as Calculator<T>;
                RegisterGlobalFunction(new Function<double, double>("double", val => val));

                RegisterGlobalFunction(new Function<double>("UniversalTime", () => Planetarium.GetUniversalTime(), false));
                RegisterGlobalFunction(new Function<double>("Funds", () => Funding.Instance != null ? Funding.Instance.Funds : 0.0, false));
                RegisterGlobalFunction(new Function<double>("StartingFunds", () => HighLogic.CurrentGame != null ? HighLogic.CurrentGame.Parameters.Career.StartingFunds : 0.0, false));
            }

            RegisterLocalFunction(new Function<T>("Random", () => (T)Convert.ChangeType(random.NextDouble(), typeof(T)), false));
            RegisterLocalFunction(new Function<T, T, T>("Random", RandomMinMax, false));
            RegisterLocalFunction(new Function<T, T, T>("Max", Max));
            RegisterLocalFunction(new Function<T, T, T>("Min", Min));

            RegisterLocalFunction(new Function<T, T, T>("Pow", Pow));
            RegisterLocalFunction(new Function<T, T, T>("Log", Log));

            RegisterLocalFunction(new Function<T, T>("Round", Round));
            RegisterLocalFunction(new Function<T, T, T>("Round", Round));

            RegisterMethod(new Method<T, string>("Print", PrintNumber));
            RegisterMethod(new Method<T, string, string>("ToString", ToString));
            
            RegisterLocalFunction(new Function<T, T>("Sin", Sin));
            RegisterLocalFunction(new Function<T, T>("Cos", Cos));
            RegisterLocalFunction(new Function<T, T>("Tan", Tan));
            RegisterLocalFunction(new Function<T, T>("Asin", Asin));
            RegisterLocalFunction(new Function<T, T>("Acos", Acos));
            RegisterLocalFunction(new Function<T, T>("Atan", Atan));
            RegisterLocalFunction(new Function<T, T>("Sinh", Sinh));
            RegisterLocalFunction(new Function<T, T>("Cosh", Cosh));
            RegisterLocalFunction(new Function<T, T>("Tanh", Tanh));
        }

        public static string PrintNumber(T tval)
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
        }

        public static string ToString(T tval, string format)
        {
            if (typeof(T) == typeof(int) || typeof(T) == typeof(short))
            {
                int ival = (int)(object)tval;
                return ival.ToString(format);
            }
            else if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
            {
                double dval = (double)(object)tval;
                return dval.ToString(format);
            }
            return tval.ToString();
        }

        public static T RandomMinMax(T min, T max)
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

        private static T Pow(T a, T b)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));
            double db = (double)Convert.ChangeType(b, typeof(double));

            double val = Math.Pow(da, db);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Log(T a, T b)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));
            double db = (double)Convert.ChangeType(b, typeof(double));

            double val = Math.Log(da, db);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Round(T value)
        {
            return Round(value, 1.0);
        }

        private static T Round(T value, T precision)
        {
            double dprecision = (double)Convert.ChangeType(precision, typeof(double));

            return Round(value, dprecision);
        }

        private static T Round(T value, double precision)
        {
            double dval = (double)Convert.ChangeType(value, typeof(double));

            double val = precision * Math.Round(dval / precision);

            return (T)Convert.ChangeType(val, typeof(T));
        }
        
        private static T Sin(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Sin(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Cos(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Cos(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Tan(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Tan(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Asin(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Asin(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Acos(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Acos(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Atan(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Atan(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Sinh(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Sinh(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Cosh(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Cosh(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        private static T Tanh(T a)
        {
            double da = (double)Convert.ChangeType(a, typeof(double));

            double val = Math.Tanh(da);
            return (T)Convert.ChangeType(val, typeof(T));
        }

        public NumericValueExpressionParser()
            : base()
        {
            if (calculator == null)
            {
                throw new NotSupportedException("Type " + typeof(T) + " is not supported!");
            }
        }

        public override Token ParseNumericConstant()
        {
            try
            {
                Match m = Regex.Match(expression, @"^(\d+(\.\d+(E(-)?\d+)?)?)");
                string strVal = m.Groups[1].Value;
                expression = (expression.Length > strVal.Length ? expression.Substring(strVal.Length) : "");

                T val;
                if (typeof(T) == typeof(float))
                {
                    val = (T)(object)Single.Parse(strVal, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowExponent);
                }
                else if (typeof(T) == typeof(double))
                {
                    val = (T)(object)Double.Parse(strVal, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowExponent);
                }
                else
                {
                    val = (T)Convert.ChangeType(strVal, typeof(T));
                }

                return new ValueToken<T>(val);
            }
            catch (Exception e)
            {
                throw new NotSupportedException("Couldn't parse numeric constant!", e);
            }
        }

        public override T Negate(T val)
        {
            return calculator.Negate(val);
        }

        public override T Add(T a, T b)
        {
            return calculator.Add(a, b);
        }

        public override T Sub(T a, T b)
        {
            return calculator.Sub(a, b);
        }

        public override T Mult(T a, T b)
        {
            return calculator.Mult(a, b);
        }

        public override T Div(T a, T b)
        {
            return calculator.Div(a, b);
        }

        public override bool EQ(T a, T b)
        {
            return calculator.EQ(a, b);
        }

        public override bool NE(T a, T b)
        {
            return calculator.NE(a, b);
        }

        /// <summary>
        /// Parses an identifier for a value stored in the persistant data store.
        /// </summary>
        /// <param name="token">Token of the identifier to parse</param>
        /// <returns>Value of the identifier</returns>
        public override T ParseIdentifier(Token token)
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
                    LoggingUtil.LogWarning(this, "Unable to retrieve value for '{0}' - PersistentDataStore is null.  This is likely caused by another ScenarioModule crashing, preventing others from loading.", token.sval);
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
