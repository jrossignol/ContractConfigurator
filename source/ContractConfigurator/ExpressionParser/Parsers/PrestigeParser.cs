using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for CelestialBody.
    /// </summary>
    public class PrestigeParser : EnumExpressionParser<Contract.ContractPrestige>, IExpressionParserRegistrer
    {
        static PrestigeParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(Contract.ContractPrestige), typeof(PrestigeParser));
        }

        internal static void RegisterMethods()
        {
            RegisterGlobalFunction(new Function<Contract.ContractPrestige>("Prestige", () =>
                ConfiguredContract.currentContract != null ? ConfiguredContract.currentContract.Prestige : Contract.ContractPrestige.Trivial, false));
        }

        public PrestigeParser()
        {
        }
    }
}
