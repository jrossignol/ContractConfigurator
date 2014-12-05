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
     * Custom version of the stock ReachDestination parameter.
     */
    public class ReachDestinationCustom : VesselParameter
    {
        protected string title { get; set; }
        public CelestialBody destination { get; set; }

        public ReachDestinationCustom()
            : this(null, "dummy title")
        {
        }

        public ReachDestinationCustom(CelestialBody destination, string title)
            : base()
        {
            this.title = title != null ? title : "Destination: " + destination.name;
            this.destination = destination;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("destination", destination.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            destination = ConfigNodeUtil.ParseCelestialBody(node, "destination");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselSOIChanged.Add(new EventData<GameEvents.HostedFromToAction<Vessel, CelestialBody>>.OnEvent(OnVesselSOIChanged));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselSOIChanged.Remove(new EventData<GameEvents.HostedFromToAction<Vessel, CelestialBody>>.OnEvent(OnVesselSOIChanged));
        }

        protected void OnVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> pair)
        {
            CheckVessel(pair.host);
        }
        
        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            return vessel.mainBody == destination;
        }
    }
}
