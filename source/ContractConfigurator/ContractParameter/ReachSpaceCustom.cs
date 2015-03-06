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
    /// Custom implementation of the ReachSpace parameter
    /// </summary>
    public class ReachSpaceCustom : ContractConfiguratorParameter
    {
        public ReachSpaceCustom()
            : base(null)
        {
        }

        public ReachSpaceCustom(string title)
            : base(title)
        {
        }

        protected override string GetTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                output = "Reach space";
            }
            else 
            {
                output = title;
            }
            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselSituationChange.Add(new EventData<GameEvents.HostedFromToAction<Vessel,Vessel.Situations>>.OnEvent(OnVesselSituationChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselSituationChange.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, Vessel.Situations>>.OnEvent(OnVesselSituationChange));
        }

        protected void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> hfta)
        {
            if (hfta.to == Vessel.Situations.SUB_ORBITAL ||
                hfta.to == Vessel.Situations.ORBITING ||
                hfta.to == Vessel.Situations.ESCAPING)
            {
                SetState(ParameterState.Complete);
            }
        }
    }
}
