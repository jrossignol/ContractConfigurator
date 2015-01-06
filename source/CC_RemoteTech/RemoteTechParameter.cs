using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using ContractConfigurator;
using RemoteTech;

namespace ContractConfigurator.RemoteTech
{
    public abstract class RemoteTechParameter : VesselParameter
    {
        public RemoteTechParameter()
            : base()
        {
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            RemoteTechAssistant.OnRemoteTechUpdate.Add(new EventData<VesselSatellite>.OnEvent(OnRemoteTechUpdate));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            RemoteTechAssistant.OnRemoteTechUpdate.Remove(new EventData<VesselSatellite>.OnEvent(OnRemoteTechUpdate));
        }

        protected void OnRemoteTechUpdate(VesselSatellite s)
        {
            CheckVessel(s.parentVessel);
        }

        /// <summary>
        /// Check for whether we are in a valid state to check the given vessel.  Checks if the
        /// RemoteTech logic is initialized.
        /// </summary>
        /// <param name="vessel">The vessel - ignored.</param>
        /// <returns>True only if RemoteTech is initialized.</returns>
        protected override bool CanCheckVesselMeetsCondition(Vessel vessel)
        {
            return (RTCore.Instance != null);
        }
    }
}
