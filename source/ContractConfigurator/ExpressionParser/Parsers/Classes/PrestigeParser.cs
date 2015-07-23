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

        internal new static void RegisterMethods()
        {
            RegisterMethod(new Method<Contract.ContractPrestige, double>("Multiplier", p => GameVariables.Instance.GetContractPrestigeFactor(p)));

            RegisterGlobalFunction(new Function<Contract.ContractPrestige>("Prestige", () =>
                ConfiguredContract.currentContract != null ? ConfiguredContract.currentContract.Prestige : Contract.ContractPrestige.Trivial, false));
            RegisterGlobalFunction(new Function<double>("ContractMultiplier", ContractMultiplier, false));
        }

        public PrestigeParser()
        {
        }

        public static double ContractMultiplier()
        {
            double multiplier = 1.0;
            if (ConfiguredContract.currentContract != null)
            {
                multiplier *= GameVariables.Instance.GetContractPrestigeFactor(ConfiguredContract.currentContract.Prestige);

                if (ConfiguredContract.currentContract.contractType != null &&
                    ConfiguredContract.currentContract.contractType.targetBody!= null)
                {
                    multiplier *= GameVariables.Instance.GetContractDestinationWeight(ConfiguredContract.currentContract.contractType.targetBody);
                }
            }

            return multiplier;
        }
    }
}
