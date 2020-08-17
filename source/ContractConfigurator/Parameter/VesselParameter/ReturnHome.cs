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
    /// Parameter for checking that a vessel is home.
    /// </summary>
    public class ReturnHome : VesselParameter
    {
        public ReturnHome()
            : this(null)
        {
        }

        public ReturnHome(string title)
            : base(title)
        {
            CelestialBody home = FlightGlobals.Bodies.Where(cb => cb.isHomeWorld).First();
            this.title = title != null ? title : Localizer.Format("#cc.param.ReturnHome", home.displayName);
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
            GameEvents.onVesselSituationChange.Add(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselSituationChange.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> pair)
        {
            CheckVessel(pair.host);
        }

        protected void OnParameterChange(Contract c, ContractParameter p)
        {
            if (c == Root)
            {
                CheckVessel(FlightGlobals.ActiveVessel);
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);
            return vessel.mainBody.isHomeWorld &&
                (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED);
        }
    }
}
