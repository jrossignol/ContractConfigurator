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
    /// ParameterFactory wrapper for MissionTimer ContractParameter.
    /// </summary>
    public class MissionTimerFactory : ParameterFactory
    {
        protected MissionTimer.StartCriteria startCriteria;
        protected MissionTimer.EndCriteria endCriteria;
        protected string startParameter;
        protected string endParameter;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get criteria
            valid &= ConfigNodeUtil.ParseValue<MissionTimer.StartCriteria>(configNode, "startCriteria", x => startCriteria = x, this, MissionTimer.StartCriteria.CONTRACT_ACCEPTANCE);
            if (startCriteria == MissionTimer.StartCriteria.PARAMETER_COMPLETION)
            {
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "startParameter", x => startParameter = x, this);
            }
            valid &= ConfigNodeUtil.ParseValue<MissionTimer.EndCriteria>(configNode, "endCriteria", x => endCriteria = x, this, MissionTimer.EndCriteria.CONTRACT_COMPLETION);
            if (endCriteria == MissionTimer.EndCriteria.PARAMETER_COMPLETION)
            {
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "endParameter", x => endParameter = x, this);
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new MissionTimer(startCriteria, endCriteria, startParameter, endParameter, title);
        }
    }
}
