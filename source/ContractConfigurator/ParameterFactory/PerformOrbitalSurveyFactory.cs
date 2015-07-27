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
    /// <summary>
    /// ParameterFactory wrapper for PerformOrbitalSurvey ContractParameter.
    /// </summary>
    public class PerformOrbitalSurveyFactory : ParameterFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Validate target body
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            // Perform another validation of the target body to catch late validation issues due to expressions
            if (!ValidateTargetBody())
            {
                return null;
            }

            return new PerformOrbitalSurvey(title, targetBody);
        }
    }
}
