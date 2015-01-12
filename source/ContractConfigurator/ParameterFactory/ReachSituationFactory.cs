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
    /*
     * ParameterFactory wrapper for ReachSituation ContractParameter.
     */
    [Obsolete("Obsolete, use ReachState")]
    public class ReachSituationFactory : ParameterFactory
    {
        protected Vessel.Situations situation;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<Vessel.Situations>(configNode, "situation", ref situation, this);

            LoggingUtil.LogError(this, "ReachSituation is obsolete as of ContractConfigurator 0.5.3, please use ReachState instead.  ReachSituation will be removed in a future release.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachSituationCustom(situation, title);
        }
    }
}
