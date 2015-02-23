using ContractConfigurator.Parameters;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using UnityEngine;

namespace ContractConfigurator.SCANsat
{
    public class SCANsatCoverageFactory : ParameterFactory
    {
        protected string scanType;
        protected double coverage;

        public override bool Load(ConfigNode configNode)
        {
            // Before loading, verify the SCANsat version
            if (!SCANsatUtil.VerifySCANsatVersion())
            {
                return false;
            }

            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "coverage", x => coverage = x, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "scanType", x => scanType = x, this, SCANsatUtil.ValidateSCANname);
            valid &= ValidateTargetBody(configNode);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new SCANsatCoverage(coverage, scanType, targetBody, title);
        }
    }
}
