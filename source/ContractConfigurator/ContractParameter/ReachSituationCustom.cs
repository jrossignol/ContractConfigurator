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
     * Custom version of the stock ReachSituation parameter.
     */
    public class ReachSituationCustom : VesselParameter
    {
        protected string title { get; set; }
        public Vessel.Situations situation { get; set; }

        public ReachSituationCustom()
            : this(Vessel.Situations.LANDED, null)
        {
        }

        public ReachSituationCustom(Vessel.Situations situation, string title)
            : base()
        {
            this.title = title != null ? title : "Situation: " + ReachSituation.GetTitleStringShort(situation);
            this.situation = situation;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("situation", situation);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            situation = (Vessel.Situations)Enum.Parse(typeof(Vessel.Situations), node.GetValue("situation"));
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
            return vessel.situation == situation;
        }
    }
}
