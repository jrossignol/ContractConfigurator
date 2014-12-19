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
    /*
     * ParameterFactory wrapper for Timer ContractParameter.
     */
    public class TimerFactory : ParameterFactory
    {
        protected double duration { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get duration
            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "duration", this);
            if (valid)
            {
                duration = DurationUtil.ParseDuration(configNode, "duration");
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Timer(duration);
        }
    }
}
