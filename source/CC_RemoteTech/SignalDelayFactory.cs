using ContractConfigurator.Parameters;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using UnityEngine;
using RemoteTech;

namespace ContractConfigurator.RemoteTech
{
    public class SignalDelayFactory : ParameterFactory
    {
        public double minSignalDelay;
        public double maxSignalDelay;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            // Before loading, verify the RemoteTech version
            valid &= Util.VerifyRemoteTechVersion();

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minSignalDelay", ref minSignalDelay, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxSignalDelay", ref maxSignalDelay, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minSignalDelay", "maxSignalDelay" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new SignalDelayParameter(minSignalDelay, maxSignalDelay, title);
        }
    }
}
