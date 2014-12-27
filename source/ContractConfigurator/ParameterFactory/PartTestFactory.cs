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
    /*
     * ParameterFactory wrapper for PartTest ContractParameter.
     */
    public class PartTestFactory : ParameterFactory
    {
        protected AvailablePart part;
        protected string notes;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part", ref part, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "notes", ref notes, this, (string)null);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new PartTest(part, notes);
        }
    }
}
