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
            // Prestige methods
            RegisterMethod(new Method<Contract.ContractPrestige, double>("Multiplier", p => GameVariables.Instance.GetContractPrestigeFactor(p)));

            // Prestige functions
            RegisterGlobalFunction(new Function<Contract.ContractPrestige>("Prestige", () =>
                ConfiguredContract.currentContract != null ? ConfiguredContract.currentContract.Prestige : Contract.ContractPrestige.Trivial, false));
            RegisterGlobalFunction(new Function<double>("ContractMultiplier", ContractMultiplier, false));
            RegisterGlobalFunction(new Function<Contract.ContractPrestige, Contract.ContractPrestige>("ContractPrestige", p => p));

            // Other stuff
            RegisterGlobalFunction(new Function<float>("Reputation", () => Reputation.CurrentRep, false));
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
