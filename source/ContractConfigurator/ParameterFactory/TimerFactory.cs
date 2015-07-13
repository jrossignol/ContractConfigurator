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
        protected Timer.TimerType timerType;
        protected string parameter;
        protected bool failContract;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get duration
            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "duration", x => duration = x, this, new Duration(0.0));
            valid &= ConfigNodeUtil.ParseValue<Timer.TimerType>(configNode, "timerType", x => timerType = x, this, Timer.TimerType.CONTRACT_ACCEPTANCE);
            if (timerType == Timer.TimerType.PARAMETER_COMPLETION)
            {
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "parameter", x => parameter = x, this);
            }
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "failContract", x => failContract = x, this, true);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Timer(duration.Value, timerType, parameter, failContract);
        }
    }
}
