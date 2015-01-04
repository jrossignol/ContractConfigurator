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
        protected double duration;
        protected string preWaitText;
        protected string waitingText;
        protected string completionText;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get duration
            string durationStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "duration", ref durationStr, this, "");
            if (durationStr != null)
            {
                duration = durationStr != "" ? DurationUtil.ParseDuration(durationStr) : 0.0;
            }
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "preWaitText", ref preWaitText, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "waitingText", ref waitingText, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completionText", ref completionText, this);
            valid &= ConfigNodeUtil.ValidateExcludedValue(configNode, "title", this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Duration(duration, preWaitText, waitingText, completionText);
        }
    }
}
