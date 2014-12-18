using ContractConfigurator.Parameters;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using UnityEngine;

namespace ContractConfigurator
{
    public class ScanSatCoverageFactory : ParameterFactory
    {
        protected int scanType { get; set; }
        protected string scanTypeName { get; set; }
        protected double coverage { get; set; }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            if (!configNode.HasValue("coverage"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'coverage'.");
            }
            else
            {
                try
                {
                    coverage = Convert.ToDouble(configNode.GetValue("coverage"));
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": can't parse value 'coverage'.");                    
                }
            }

            if (!configNode.HasValue("scanType"))
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": missing required value 'scanType'.");
            }
            else
            {
                try
                {
                    scanTypeName = configNode.GetValue("scanType");
                    SCANsat.SCANdata.SCANtype scanTypeEnum = (SCANsat.SCANdata.SCANtype)Enum.Parse(typeof(SCANsat.SCANdata.SCANtype), scanTypeName);
                    scanType = (int)scanTypeEnum;
                }
                catch (Exception e)
                {
                    valid = false;
                    Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": can't parse value 'scanType', wrong name?");
                }
            }

            // Validate target body
            if (targetBody == null)
            {
                valid = false;
                Debug.LogError("ContractConfigurator: " + ErrorPrefix(configNode) +
                    ": targetBody for CollectScience must be specified.");
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            Debug.Log("ScanSatCoverageFactory in Generate " + coverage + ":" + scanTypeName + ":" + targetBody + ":" + title);
            return new ScanSatCoverage(coverage, scanType, scanTypeName, targetBody, title);
        }
    }
}
