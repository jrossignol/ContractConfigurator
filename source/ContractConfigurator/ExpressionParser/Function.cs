using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Public interface for defining a function that can be called from within an expression.
    /// </summary>
    public interface Function
    {
        string Name { get; }
        bool Deterministic { get; }

        object Invoke(object[] parameters);
        int ParameterCount();
        Type ParameterType(int i);
    }

    public class MethodMismatch : Exception
    {
        public MethodMismatch(IEnumerable<Function> funcs)
            : base(GetMessage(funcs))
        {
        }

        static string GetMessage(IEnumerable<Function> funcs)
        {
            Function first = funcs.First();

            string val = "Couldn't find a matching signature for " +
                (first.GetType().GetInterface("Method`1") == null ? "function" : "method") +
                " with name '" + first.Name + "'.  Candidates are:";

            foreach (Function f in funcs)
            {
                val += "\n" + f.Name + "(";
                for (int i = 0; i < f.ParameterCount(); i++)
                {
                    if (i != 0)
                    {
                        val += ", ";
                    }
                    val += f.ParameterType(i);
                }
                val += ")";
            }

            return val;
        }
    }

    public class Function<TResult> : Function
    {
        public string Name { get; private set; }
        public bool Deterministic { get; private set; }
        protected Func<TResult> Func;

        public Function(string name, Func<TResult> function, bool deterministic = true)
        {
            Name = name;
            Func = function;
            Deterministic = deterministic;
        }

        public object Invoke(object[] parameters)
        {
            return Invoke();
        }

        public TResult Invoke()
        {
            return Func.Invoke();
        }

        public int ParameterCount()
        {
            return 0;
        }

        public Type ParameterType(int i)
        {
            throw new NotSupportedException();
        }
    }

    public class Function<T, TResult> : Function
    {
        public string Name { get; private set; }
        public bool Deterministic { get; private set; }
        protected Func<T, TResult> Func;

        public Function(string name, Func<T, TResult> function, bool deterministic = true)
        {
            Name = name;
            Func = function;
            Deterministic = deterministic;
        }

        public object Invoke(object[] parameters)
        {
            return Invoke((T)parameters[0]);
        }

        public TResult Invoke(T p)
        {
            return Func.Invoke(p);
        }

        public int ParameterCount()
        {
            return 1;
        }

        public Type ParameterType(int i)
        {
            switch (i)
            {
                case 0:
                    return typeof(T);
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Function<T1, T2, TResult> : Function
    {
        public string Name { get; private set; }
        public bool Deterministic { get; private set; }
        protected Func<T1, T2, TResult> Func;

        public Function(string name, Func<T1, T2, TResult> function, bool deterministic = true)
        {
            Name = name;
            Func = function;
            Deterministic = deterministic;
        }

        public object Invoke(object[] parameters)
        {
            return Invoke((T1)parameters[0], (T2)parameters[1]);
        }

        public TResult Invoke(T1 p1, T2 p2)
        {
            return Func.Invoke(p1, p2);
        }

        public int ParameterCount()
        {
            return 2;
        }

        public Type ParameterType(int i)
        {
            switch (i)
            {
                case 0:
                    return typeof(T1);
                case 1:
                    return typeof(T2);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
