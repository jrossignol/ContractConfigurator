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
    /*
     * Parameter for checking that a vessel is home.
     */
    public class ReturnHome : VesselParameter
    {
        protected string title { get; set; }

        public ReturnHome()
            : this(null)
        {
        }

        public ReturnHome(string title)
            : base()
        {
            this.title = title != null ? title : "Return home";
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselSituationChange.Add(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselSituationChange.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
        }

        protected void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> pair)
        {
            CheckVessel(pair.host);
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return vessel.mainBody.isHomeWorld &&
                (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED);
        }
    }
}
