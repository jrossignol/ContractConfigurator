using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        protected static void RegisterMethods()
        {
            RegisterMethod(new Method<List<T>, T>("Random", l => l.Skip(r.Next(l.Count)).First()));
        }

        public ListExpressionParser()
        {
        }
    }
}
