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
        protected Parameters.Duration.StartCriteria startCriteria;
        protected List<string> parameter;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "duration", x => duration = x, this, new Duration(0.0));
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "preWaitText", x => preWaitText = x, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "waitingText", x => waitingText = x, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "completionText", x => completionText = x, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<Parameters.Duration.StartCriteria>(configNode, "startCriteria", x => startCriteria = x, this, Parameters.Duration.StartCriteria.CONTRACT_ACCEPTANCE);
            valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this, new List<string>());
            valid &= ConfigNodeUtil.ValidateExcludedValue(configNode, "title", this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.Duration(duration.Value, preWaitText, waitingText, completionText, startCriteria, parameter);
        }
    }
}
