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
    public class ActiveVesselConnectionFactory : ParameterFactory
    {
        protected double range;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "range", ref range, this, x => Validation.GT(x, 0.0));

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ActiveVesselConnectionParameter(range, title);
        }
    }
}
