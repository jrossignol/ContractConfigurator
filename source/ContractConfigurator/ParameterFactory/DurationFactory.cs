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
    /// ParameterFactory wrapper for Duration ContractParameter.
    /// </summary>
    public class DurationFactory : ParameterFactory
    {
        protected Duration duration;
        protected string preWaitText;
        protected string waitingText;
        protected string completionText;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "duration", x => duration = x, this, new Duration(0.0));
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "preWaitText", x => preWaitText = x, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "waitingText", x => waitingText = x, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completionText", x => completionText = x, this);
            valid &= ConfigNodeUtil.ValidateExcludedValue(configNode, "title", this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.Duration2(duration.Value, preWaitText, waitingText, completionText);
        }
    }
}
