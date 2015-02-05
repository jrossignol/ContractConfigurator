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
    /// <summary>
    /// Parameter for a vessel having visited a CelestialBody + Situation.
    /// </summary>
    public class VesselHasVisited : VesselParameter
    {
        protected string title { get; set; }
        public CelestialBody destination { get; set; }
        public FlightLog.EntryType entryType { get; set; }

        public VesselHasVisited()
            : this(null, FlightLog.EntryType.Flight, null)
        {
        }

        public VesselHasVisited(CelestialBody destination, FlightLog.EntryType entryType, string title)
            : base()
        {
            if (title == null)
            {
                this.title = "Perform ";
                switch (entryType)
                {
                    case FlightLog.EntryType.BoardVessel:
                        this.title = "Board a vessel on ";
                        break;
                    case FlightLog.EntryType.Die:
                        this.title = "Die on ";
                        break;
                    case FlightLog.EntryType.Escape:
                        this.title += "an escape from";
                        break;
                    case FlightLog.EntryType.ExitVessel:
                        this.title = "Exit a vessel on ";
                        break;
                    case FlightLog.EntryType.Flight:
                        this.title += "a flight on ";
                        break;
                    case FlightLog.EntryType.Flyby:
                        this.title += "a flyby of ";
                        break;
                    case FlightLog.EntryType.Land:
                        this.title += "a landing on ";
                        break;
                    case FlightLog.EntryType.Launch:
                        this.title += "a launch from ";
                        break;
                    case FlightLog.EntryType.Orbit:
                        this.title += "an orbit of ";
                        break;
                    case FlightLog.EntryType.PlantFlag:
                        this.title = "Plant a flag on ";
                        break;
                    case FlightLog.EntryType.Recover:
                        this.title += " a recovery on ";
                        break;
                    case FlightLog.EntryType.Spawn:
                        this.title = "Spawn on ";
                        break;
                    case FlightLog.EntryType.Suborbit:
                        this.title += "a sub-orbital trajectory of ";
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
            this.entryType = entryType;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("title", title);
            node.AddValue("destination", destination.name);
            node.AddValue("entryType", entryType);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            title = node.GetValue("title");
            destination = ConfigNodeUtil.ParseValue<CelestialBody>(node, "destination");
            entryType = (FlightLog.EntryType)Enum.Parse(typeof(FlightLog.EntryType), node.GetValue("entryType"));
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

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            return VesselTripLog.FromVessel(vessel).Log.HasEntry(entryType, destination.name);
        }
    }
}
