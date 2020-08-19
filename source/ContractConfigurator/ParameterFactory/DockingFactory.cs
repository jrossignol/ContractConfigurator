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
    /// ParameterFactory wrapper for Docking ContractParameter. 
    /// </summary>
    public class DockingFactory : ParameterFactory
    {
        protected List<VesselIdentifier> vessels;
        protected VesselIdentifier defineDockedVessel;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<List<VesselIdentifier>>(configNode, "vessel", x => vessels = x, this, new List<VesselIdentifier>());
            valid &= ConfigNodeUtil.ParseValue<VesselIdentifier>(configNode, "defineDockedVessel", x => defineDockedVessel = x, this, (VesselIdentifier)null);

            // Validate using the config node instead of the list as it may undergo deferred loading
            if (parent is VesselParameterGroupFactory)
            {
                if (configNode.GetValues("vessel").Count() > 1)
                {
                    LoggingUtil.LogError(this, "{0}: When used under a VesselParameterGroup, no more than one vessel may be specified for the Docking parameter.", ErrorPrefix());
                    valid = false;
                }
            }
            else
            {
                if (configNode.GetValues("vessel").Count() == 0)
                {
                    LoggingUtil.LogError(this, "{0}: Need at least one vessel specified for the Docking parameter.", ErrorPrefix());
                    valid = false;
                }
                if (configNode.GetValues("vessel").Count() > 2)
                {
                    LoggingUtil.LogError(this, "{0}: Cannot specify more than two vessels for the Docking parameter.", ErrorPrefix());
                    valid = false;
                }
            }

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new Docking(vessels.Select<VesselIdentifier, string>(vi => vi.identifier), defineDockedVessel != null ? defineDockedVessel.identifier : null, title);
        }
    }
}
