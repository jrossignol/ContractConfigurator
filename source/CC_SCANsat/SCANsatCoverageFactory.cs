using ContractConfigurator.Parameters;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using UnityEngine;
using SCANsat;

namespace ContractConfigurator.SCANsat
{
    public class SCANsatCoverageFactory : ParameterFactory
    {
        protected int scanType { get; set; }
        protected string scanTypeName { get; set; }
        protected double coverage { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "coverage", this);
            if (valid)
            {
                try
                {
                    coverage = Convert.ToDouble(configNode.GetValue("coverage"));
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": can't parse value 'coverage': " + e.Message);                    
                }
            }

            valid &= ConfigNodeUtil.ValidateMandatoryField(configNode, "scanType", this);
            if (valid)
            {
                try
                {
                    scanTypeName = configNode.GetValue("scanType");
                    SCANdata.SCANtype scanTypeEnum = (SCANdata.SCANtype)Enum.Parse(typeof(SCANdata.SCANtype), scanTypeName);
                    scanType = (int)scanTypeEnum;
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": can't parse value 'scanType':" + e.Message);
                }
            }

            // Validate target body
            if (targetBody == null)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody for SCANsatCoverage must be specified.");
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new SCANsatCoverage(coverage, scanType, scanTypeName, targetBody, title);
        }
    }
}
