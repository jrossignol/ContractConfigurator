using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for Timer ContractParameter.
    /// </summary>
    public class TimerFactory : ParameterFactory
    {
        protected Duration duration;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get duration
            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "duration", x => duration = x, this, new Duration(0.0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Timer(duration.Value);
        }
    }
}
