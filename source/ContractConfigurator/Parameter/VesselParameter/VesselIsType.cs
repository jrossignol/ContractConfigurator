using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking the VesselType of vessel.
    /// </summary>
    public class VesselIsType : VesselParameter
    {
        protected VesselType vesselType { get; set; }

        public VesselIsType()
            : base(null)
        {
        }

        public VesselIsType(VesselType vesselType, string title = null)
            : base(title)
        {
            this.vesselType = vesselType;
            this.title = title;
        }

        protected override string GetParameterTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                output = Localizer.Format("#cc.param.VesselIsType", vesselType.displayDescription());
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
            node.AddValue("vesselType", vesselType);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            vesselType = ConfigNodeUtil.ParseValue<VesselType>(node, "vesselType");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(OnVesselWasModified));
            GameEvents.onVesselRename.Add(new EventData<GameEvents.HostedFromToAction<Vessel,string>>.OnEvent(OnVesselRename));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWasModified.Remove(new EventData<Vessel>.OnEvent(OnVesselWasModified));
            GameEvents.onVesselRename.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, string>>.OnEvent(OnVesselRename));
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

        protected void OnVesselRename(GameEvents.HostedFromToAction<Vessel, string> hfta)
        {
            CheckVessel(hfta.host);
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
