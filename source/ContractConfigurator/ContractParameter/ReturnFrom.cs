using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /*
     * Parameter for returning from a CelestialBody + Situation.
     */
    public class ReturnFrom : VesselParameter
    {
        protected string title { get; set; }
        public CelestialBody destination { get; set; }
        public KSPAchievements.ReturnFrom returnFrom { get; set; }

        public ReturnFrom()
            : this(null, KSPAchievements.ReturnFrom.Flight, null)
        {
        }

        public ReturnFrom(CelestialBody destination, KSPAchievements.ReturnFrom returnFrom, string title)
            : base()
        {
            if (title == null)
            {
                this.title = "Return from ";
                switch (returnFrom)
                {
                    case KSPAchievements.ReturnFrom.Flight:
                        this.title += "flight on ";
                        break;
                    case KSPAchievements.ReturnFrom.FlyBy:
                        this.title += "flyby of ";
                        break;
                    case KSPAchievements.ReturnFrom.Orbit:
                        this.title += "orbit of ";
                        break;
                    case KSPAchievements.ReturnFrom.SubOrbit:
                        this.title += "a sub-orbital trajectory of ";
                        break;
                    case KSPAchievements.ReturnFrom.Surface:
                        this.title += "the surface of ";
                        break;
                }
                if (destination != null)
                {
                    if (destination.name == "Mun")
                    {
                        this.title += "the ";
                    }
                    this.title += destination.name;
                }
            }
            else
            {
                this.title = title;
            }
            this.destination = destination;
            this.returnFrom = returnFrom;
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
            node.AddValue("returnFrom", returnFrom);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            destination = ConfigNodeUtil.ParseCelestialBody(node, "destination");
            returnFrom = (KSPAchievements.ReturnFrom)Enum.Parse(typeof(KSPAchievements.ReturnFrom), node.GetValue("returnFrom"));
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
            // On Kerbin
            if (vessel.mainBody.isHomeWorld)
            {
                // Landed or splashed down
                if (vessel.situation == Vessel.Situations.LANDED ||
                    vessel.situation == Vessel.Situations.SPLASHED)
                {
                    VesselTripLog log = VesselTripLog.FromVessel(vessel);
                    switch (returnFrom)
                    {
                        case KSPAchievements.ReturnFrom.Flight:
                            return log.Flew.At(destination);
                        case KSPAchievements.ReturnFrom.FlyBy:
                            return log.FlewBy.At(destination);
                        case KSPAchievements.ReturnFrom.Orbit:
                            return log.Orbited.At(destination);
                        case KSPAchievements.ReturnFrom.SubOrbit:
                            return log.SubOrbited.At(destination);
                        case KSPAchievements.ReturnFrom.Surface:
                            return log.Surfaced.At(destination);
                    }
                }
            }

            return false;
        }
    }
}
