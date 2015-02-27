using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.ExpressionParser
{
    public class Method<C, TResult> : Function
    {
        public string Name { get; set; }
        protected Func<C, TResult> MethodBody;

        public Method(string name, Func<C, TResult> methodBody)
        {
            Name = name;
            MethodBody = methodBody;
        }

        public object Invoke(object[] parameters)
        {
            return Invoke((C)parameters[0]);
        }

        public TResult Invoke(C obj)
        {
            return MethodBody.Invoke(obj);
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

    public class Method<C, T, TResult> : Function
    {
        public string Name { get; set; }
        protected Func<C, T, TResult> MethodBody;

        public Method(string name, Func<C, T, TResult> methodBody)
        {
            Name = name;
            MethodBody = methodBody;
        }

        public object Invoke(object[] parameters)
        {
            return Invoke((C)parameters[0], (T)parameters[1]);
        }

        public TResult Invoke(C obj, T p)
        {
            return MethodBody.Invoke(obj, p);
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

    public class Method<C, T1, T2, TResult> : Function
    {
        public string Name { get; set; }
        protected Func<C, T1, T2, TResult> MethodBody;

        public Method(string name, Func<C, T1, T2, TResult> methodBody)
        {
            Name = name;
            MethodBody = methodBody;
        }

        public object Invoke(object[] parameters)
        {
            return Invoke((C)parameters[0], (T1)parameters[1], (T2)parameters[2]);
        }

        public TResult Invoke(C obj, T1 p1, T2 p2)
        {
            return MethodBody.Invoke(obj, p1, p2);
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
