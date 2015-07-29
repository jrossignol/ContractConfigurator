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
    /// ParameterFactory to provide logic for a parameter that groups vessel related parameters together.
    /// </summary>
    public class VesselParameterGroupFactory : ParameterFactory
    {
        protected Duration duration;
        protected string define;
        protected List<VesselIdentifier> vesselList;
        protected bool dissassociateVesselsOnContractFailure;
        protected bool dissassociateVesselsOnContractCompletion;

        public IEnumerable<string> Vessel { get { return vesselList.Select<VesselIdentifier, string>(vi => vi.identifier); } }

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<Duration>(configNode, "duration", x => duration = x, this, new Duration(0.0));
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "define", x => define = x, this, (string)null);
            valid &= ConfigNodeUtil.ParseValue<List<VesselIdentifier>>(configNode, "vessel", x => vesselList = x, this, new List<VesselIdentifier>());
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "dissassociateVesselsOnContractFailure", x => dissassociateVesselsOnContractFailure = x, this, true);
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "dissassociateVesselsOnContractCompletion", x => dissassociateVesselsOnContractCompletion = x, this, false);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.VesselParameterGroup(title, define, Vessel, duration.Value, dissassociateVesselsOnContractFailure, dissassociateVesselsOnContractCompletion);
        }
    }
}
