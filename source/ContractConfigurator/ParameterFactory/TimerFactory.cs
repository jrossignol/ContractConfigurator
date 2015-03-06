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
        protected double duration;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get duration
            string durationStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "duration", x => durationStr = x, this, "");
            if (durationStr != null)
            {
                duration = durationStr != "" ? DurationUtil.ParseDuration(durationStr) : 0.0;
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Timer(duration);
        }
    }
}
