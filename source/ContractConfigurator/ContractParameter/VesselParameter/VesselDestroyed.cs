using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking that a vessel gets destroyed.
    /// </summary>
    public class VesselDestroyed : VesselParameter
    {
        public VesselDestroyed()
            : base(null)
        {
        }

        public VesselDestroyed(string title)
            : base(title)
        {
        }

        protected override string GetTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                output = "Vessel Destroyed";
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselWillDestroy.Add(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWillDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
        }

        protected virtual void OnVesselWillDestroy(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselWillDestroy: " + v.id);
            SetState(v, ParameterState.Complete);
            CheckVessel(v);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Always true</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            return GetStateForVessel(vessel) == ParameterState.Complete;
        }
    }
}
