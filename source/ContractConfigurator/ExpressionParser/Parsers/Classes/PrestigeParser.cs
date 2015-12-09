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

        public new static void RegisterMethods()
        {
            // Prestige methods
            RegisterMethod(new Method<Contract.ContractPrestige, double>("Multiplier", p => GameVariables.Instance.GetContractPrestigeFactor(p)));

            // Prestige functions
            RegisterGlobalFunction(new Function<Contract.ContractPrestige>("Prestige", () =>
                ConfiguredContract.currentContract != null ? ConfiguredContract.currentContract.Prestige : Contract.ContractPrestige.Trivial, false));
            RegisterGlobalFunction(new Function<double>("ContractMultiplier", ContractMultiplier, false));
        }

        public PrestigeParser()
        {
        }

        public static double ContractMultiplier()
        {
            DataNode rootNode = currentParser.currentDataNode.Root;
            ExpressionParser<double> parser = BaseParser.GetParser<double>();
            return parser.ParseExpression(currentParser.currentKey, "Prestige().Multiplier() * @/targetBody.Multiplier()", currentParser.currentDataNode);
        }
    }
}
