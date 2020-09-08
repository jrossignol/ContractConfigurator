using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for a vessel having visited a CelestialBody + Situation.
    /// </summary>
    public class VesselHasVisited : VesselParameter
    {
        public CelestialBody destination { get; set; }
        public FlightLog.EntryType entryType { get; set; }

        public VesselHasVisited()
            : this(null, FlightLog.EntryType.Flight, null)
        {
        }

        public VesselHasVisited(CelestialBody destination, FlightLog.EntryType entryType, string title)
            : base(title)
        {
            if (title == null)
            {
                string bodyStr = (destination != null) ? destination.displayName : Localizer.GetStringByTag("#cc.anyBody").ToLower();
                switch (entryType)
                {
                    case FlightLog.EntryType.BoardVessel:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.BoardVessel", bodyStr);
                        break;
                    case FlightLog.EntryType.Die:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Die", bodyStr);
                        break;
                    case FlightLog.EntryType.Escape:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Escape", bodyStr);
                        break;
                    case FlightLog.EntryType.ExitVessel:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.ExitVessel", bodyStr);
                        break;
                    case FlightLog.EntryType.Flight:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Flight", bodyStr);
                        break;
                    case FlightLog.EntryType.Flyby:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Flyby", bodyStr);
                        break;
                    case FlightLog.EntryType.Land:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Land", bodyStr);
                        break;
                    case FlightLog.EntryType.Launch:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Launch", bodyStr);
                        break;
                    case FlightLog.EntryType.Orbit:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Orbit", bodyStr);
                        break;
                    case FlightLog.EntryType.PlantFlag:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.PlantFlag", bodyStr);
                        break;
                    case FlightLog.EntryType.Recover:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Recover", bodyStr);
                        break;
                    case FlightLog.EntryType.Spawn:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Spawn", bodyStr);
                        break;
                    case FlightLog.EntryType.Suborbit:
                        this.title = Localizer.Format("#cc.param.VesselHasVisited.Suborbit", bodyStr);
                        break;
                }
                if (destination != null)
                {
                    this.title += destination.displayName;
                }
                else
                {
                    this.title += "any body";
                }
            }
            else
            {
                this.title = title;
            }
            this.destination = destination;
            this.entryType = entryType;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("destination", destination.name);
            node.AddValue("entryType", entryType);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
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
