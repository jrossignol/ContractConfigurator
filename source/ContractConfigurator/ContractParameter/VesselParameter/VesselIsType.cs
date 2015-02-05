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
    /// Parameter for checking the VesselType of vessel.
    /// </summary>
    public class VesselIsType : VesselParameter
    {
        protected string title { get; set; }
        protected VesselType vesselType { get; set; }

        public VesselIsType()
            : base()
        {
        }

        public VesselIsType(VesselType vesselType, string title = null)
            : base()
        {
            this.vesselType = vesselType;
            this.title = title;
        }

        protected override string GetTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                output = "Vessel type: " + vesselType;
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
            node.AddValue("title", title);
            node.AddValue("vesselType", vesselType);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            title = node.GetValue("title");
            vesselType = ConfigNodeUtil.ParseValue<VesselType>(node, "vesselType");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWasModified.Remove(new EventData<Vessel>.OnEvent(OnVesselWasModified));
        }

        protected override void OnFlightReady()
        {
 	        base.OnFlightReady();
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnVesselCreate(Vessel vessel)
        {
            base.OnVesselCreate(vessel);
            CheckVessel(vessel);
        }

        protected override void OnVesselChange(Vessel vessel)
        {
            base.OnVesselChange(vessel);
            CheckVessel(vessel);
        }

        protected void OnVesselWasModified(Vessel vessel)
        {
            CheckVessel(vessel);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return vessel.vesselType == vesselType;
        }
    }
}
