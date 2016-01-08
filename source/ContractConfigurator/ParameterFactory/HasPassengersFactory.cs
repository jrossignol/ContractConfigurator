using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for HasPassengers ContractParameter.
    /// </summary>
    public class HasPassengersFactory : ParameterFactory
    {
        protected int index;
        protected int count;
        protected List<Kerbal> passengers;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "index", x => index = x, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "count", x => count = x, this, 0, x => Validation.GE(x, 0));
            valid &= ConfigNodeUtil.ParseValue<List<Kerbal>>(configNode, "kerbal", x => passengers = x, this, new List<Kerbal>());

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            if (passengers.Count() > 0)
            {
                return new HasPassengers(title, passengers);
            }
            else
            {
                return new HasPassengers(title, index, count);
            }
        }
    }
}
