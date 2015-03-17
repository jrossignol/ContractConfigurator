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
        protected bool mustImpactTerrain = false;

        public VesselDestroyed()
            : base(null)
        {
        }

        public VesselDestroyed(string title, bool mustImpactTerrain)
            : base(title)
        {
            this.mustImpactTerrain = mustImpactTerrain;
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

            node.AddValue("mustImpactTerrain", mustImpactTerrain);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            mustImpactTerrain = ConfigNodeUtil.ParseValue<bool?>(node, "mustImpactTerrain", (bool?)false).Value;
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onCollision.Add(new EventData<EventReport>.OnEvent(OnVesselAboutToBeDestroyed));
            GameEvents.onCrash.Add(new EventData<EventReport>.OnEvent(OnVesselAboutToBeDestroyed));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onCollision.Remove(new EventData<EventReport>.OnEvent(OnVesselAboutToBeDestroyed));
            GameEvents.onCrash.Remove(new EventData<EventReport>.OnEvent(OnVesselAboutToBeDestroyed));
        }

        protected virtual void OnVesselAboutToBeDestroyed(EventReport report)
        {
            LoggingUtil.LogVerbose(this, "OnVesselAboutToBeDestroyed: " + report);
            Vessel v = report.origin.vessel;
            if (v == null)
            {
                return;
            }

            // Check if we hit the ground
            if (mustImpactTerrain)
            {
                if (!(
                    report.other.ToLower().Contains(string.Intern("surface")) ||
                    report.other.ToLower().Contains(string.Intern("terrain")) ||
                    report.other.ToLower().Contains(v.mainBody.name.ToLower())))
                {
                    return;
                }
            }

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
