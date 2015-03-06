using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for AltitudeRecord ContractParameter.
    /// </summary>
    public class AltitudeRecordFactory : ParameterFactory
    {
        protected double altitude;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Get altitude
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "altitude", x => altitude = x, this, x => Validation.GT(x, 0.0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new AltitudeRecord(altitude);
        }
    }
}
