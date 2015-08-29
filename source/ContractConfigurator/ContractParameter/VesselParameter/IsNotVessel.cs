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
    /// Parameter for checking that a vessel is not a given vessel.
    /// </summary>
    public class IsNotVessel : VesselParameter
    {
        protected string vesselKey { get; set; }

        public IsNotVessel()
            : this("", null)
        {
        }

        public IsNotVessel(string vesselKey, string title)
            : base(title)
        {
            failWhenUnmet = true;
            fakeFailures = true;

            this.vesselKey = vesselKey;
        }

        protected override string GetParameterTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                output = "Vessel: Not " + ContractVesselTracker.GetDisplayName(vesselKey);
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
            node.AddValue("vesselKey", vesselKey);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            vesselKey = node.GetValue("vesselKey");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            ContractVesselTracker.OnVesselAssociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Add(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
            GameEvents.onVesselRename.Add(new EventData<GameEvents.HostedFromToAction<Vessel, string>>.OnEvent(OnVesselRename));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            ContractVesselTracker.OnVesselAssociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselAssociation));
            ContractVesselTracker.OnVesselDisassociation.Remove(new EventData<GameEvents.HostTargetAction<Vessel, string>>.OnEvent(OnVesselDisassociation));
            GameEvents.onVesselRename.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, string>>.OnEvent(OnVesselRename));
        }

        protected void OnVesselAssociation(GameEvents.HostTargetAction<Vessel, string> pair)
        {
            LoggingUtil.LogVerbose(this, "OnVesselAssociation");
            CheckVessel(pair.host);
        }

        protected void OnVesselDisassociation(GameEvents.HostTargetAction<Vessel, string> pair)
        {
            LoggingUtil.LogVerbose(this, "OnVesselDisassociation");
            CheckVessel(pair.host);
        }

        protected void OnVesselRename(GameEvents.HostedFromToAction<Vessel, string> hft)
        {
            // Force a title update if it's the vessel we're looking at
            Vessel v = ContractVesselTracker.Instance.GetAssociatedVessel(vesselKey);
            if (v == hft.host)
            {
                GetTitle();
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return ContractVesselTracker.Instance.GetAssociatedVessel(vesselKey) != vessel;
        }
    }
}
