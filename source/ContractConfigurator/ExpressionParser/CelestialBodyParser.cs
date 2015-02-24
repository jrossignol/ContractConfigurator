using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for CelestialBody.
    /// </summary>
    public class CelestialBodyParser : ClassExpressionParser<CelestialBody>, IExpressionParserRegistrer
    {
        public void RegisterExpressionParsers()
        {
            ExpressionParserUtil.RegisterParserType(typeof(CelestialBody), typeof(CelestialBodyParser));
        }

        public CelestialBodyParser()
        {
        }

        protected override U ConvertType<U>(CelestialBody value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.PrintName();
            }
            return base.ConvertType<U>(value);
        }

        protected override CelestialBody ParseIdentifier(Token token)
        {
            return ConfigNodeUtil.ParseCelestialBodyValue(token.sval);
        }
    }
}
