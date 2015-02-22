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
        private Calculator<T> calculator;

        public NumericValueExpressionParser()
            : base()
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
            else
            {
                throw new NotSupportedException("Type " + typeof(T) + " is not supported!");
            }
        }

        protected override Token ParseNumericConstant()
        {
            int index = expression.IndexOfAny(ExpressionParserUtil.WHITESPACE_OR_OPERATOR, 0);

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

        protected override T Negate(T val)
        {
            return calculator.Negate(val);
        }

        protected override T Add(T a, T b)
        {
            return calculator.Add(a, b);
        }

        protected override T Sub(T a, T b)
        {
            return calculator.Sub(a, b);
        }

        protected override T Mult(T a, T b)
        {
            return calculator.Mult(a, b);
        }

        protected override T Div(T a, T b)
        {
            return calculator.Div(a, b);
        }

        protected override bool EQ(T a, T b)
        {
            return calculator.EQ(a, b);
        }

        protected override bool NE(T a, T b)
        {
            return calculator.NE(a, b);
        }
    }
}
