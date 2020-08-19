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
    /// Parameter for planting a flag.
    /// </summary>
    public class PlantFlagCustom : VesselParameter
    {
        public PlantFlagCustom()
            : base(null)
        {
        }

        public PlantFlagCustom(CelestialBody targetBody, string title)
            : base(title)
        {
            this.title = title;
            this.targetBody = targetBody;
        }

        protected override string GetParameterTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                output = Localizer.Format("#autoLOC_284213", targetBody.displayName);
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

            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onFlagPlant.Add(new EventData<Vessel>.OnEvent(OnPlantFlag));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onFlagPlant.Remove(new EventData<Vessel>.OnEvent(OnPlantFlag));
        }

        protected void OnPlantFlag(Vessel v)
        {
            if (targetBody == FlightGlobals.ActiveVessel.mainBody)
            {
                SetState(FlightGlobals.ActiveVessel, ParameterState.Complete);

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

            return GetStateForVessel(vessel) == ParameterState.Complete;
        }
    }
}
