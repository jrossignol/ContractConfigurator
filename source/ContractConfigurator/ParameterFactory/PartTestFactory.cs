using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Behaviour;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for PartTest ContractParameter.
    /// </summary>
    public class PartTestFactory : ParameterFactory
    {
        protected AvailablePart part;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", x => part = x, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            ConfiguredContract cc = contract as ConfiguredContract;
            if (!cc.Behaviours.Any(cb => cb is PartTestHandler))
            {
                cc.AddBehaviour(new PartTestHandler());
            }

            System.Random random = new System.Random();
            return new PartTest(part, notes, PartTestConstraint.TestRepeatability.ONCEPERPART, targetBody, Vessel.Situations.LANDED, random.NextDouble().ToString(), false);
        }
    }
}
