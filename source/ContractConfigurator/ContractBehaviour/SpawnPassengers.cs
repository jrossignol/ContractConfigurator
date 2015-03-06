using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Class for spawning passengers.
    /// </summary>
    public class SpawnPassengers : ContractBehaviour, IHasKerbalBehaviour
    {
        public static EventVoid onPassengersLoaded = new EventVoid("onPassengersLoaded");

        private class PassengerLoader : MonoBehaviour
        {
            private string contractTitle = "";
            private int passengerCount = 0;
            private SpawnPassengers behaviourReference = null;
            private bool uiHidden = false;
            private bool visible = false;
            private Rect windowPos = new Rect((Screen.width - 200) / 2, (Screen.height - 120) / 2, 200, 120);

            protected void Start()
            {
                GameEvents.onHideUI.Add(new EventVoid.OnEvent(OnHideUI));
                GameEvents.onShowUI.Add(new EventVoid.OnEvent(OnShowUI));
            }

            protected void OnDestroy()
            {
                GameEvents.onHideUI.Remove(OnHideUI);
                GameEvents.onShowUI.Remove(OnShowUI);
            }

            public void Show(SpawnPassengers parameterReference, string contractTitle, int passengerCount)
            {
                visible = true;
                this.behaviourReference = parameterReference;
                this.contractTitle = contractTitle;
                this.passengerCount = passengerCount;
            }

            public void OnHideUI()
            {
                uiHidden = true;
            }

            public void OnShowUI()
            {
                uiHidden = false;
            }

            public void OnGUI()
            {
                if (visible && !uiHidden)
                {
                    GUI.skin = HighLogic.Skin;
                    windowPos = GUILayout.Window(
                        GetType().FullName.GetHashCode(),
                        windowPos,
                        PassengerDialog,
                        "Load Passengers?",
                        GUILayout.Width(320),
                        GUILayout.Height(120));
                }
            }

            void PassengerDialog(int windowID)
            {
                GUILayout.BeginVertical();

                GUILayout.Label("The contract '" + contractTitle + "' requires " + passengerCount +
                    " passenger" + (passengerCount > 1 ? "s" : "") + ".  Would you like to load them onto this vessel?");

                if (GUILayout.Button("Yes"))
                {
                    behaviourReference.AddPassengersToActiveVessel();
                    visible = false;
                    Destroy(this);
                }

                if (GUILayout.Button("No"))
                {
                    visible = false;
                    Destroy(this);
                }

                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        protected int count { get; set; }
        protected List<string> passengerNames { get; set; }
        protected bool passengersLoaded = false;

        private List<ProtoCrewMember> passengers = new List<ProtoCrewMember>();

        public int KerbalCount { get { return passengers.Count; } }

        public SpawnPassengers() {}

        public SpawnPassengers(List<string> passengerNames, int minPassengers = 1)
        {
            this.passengerNames = passengerNames;
            this.count = passengerNames.Count != 0 ? passengerNames.Count : minPassengers;
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
        }

        protected void OnFlightReady()
        {
            // Checks for dispalying the dialog box
            Vessel v = FlightGlobals.ActiveVessel;
            if (v != null && v.situation == Vessel.Situations.PRELAUNCH &&
                !passengersLoaded &&
                v.GetCrewCapacity() - v.GetCrewCount() >= count)
            {
                PassengerLoader loader = MapView.MapCamera.gameObject.GetComponent<PassengerLoader>();
                if (loader == null)
                {
                    LoggingUtil.LogVerbose(this, "Adding PassengerLoader");
                    loader = MapView.MapCamera.gameObject.AddComponent<PassengerLoader>();
                }

                loader.Show(this, contract.Title, count);
            }
        }

        private void OnVesselRecovered(ProtoVessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselRecovered: " + v);

            // EVA vessel
            if (v.vesselType == VesselType.EVA)
            {
                foreach (ProtoPartSnapshot p in v.protoPartSnapshots)
                {
                    foreach (string name in p.protoCrewNames)
                    {
                        // Find this crew member in our data and remove them
                        ProtoCrewMember passenger = passengers.Where(pcm => pcm.name == name).FirstOrDefault();
                        if (passenger != null)
                        {
                            // I would like to remove the passengers from existance, but then there
                            // is a small chance of KSP failing if the passengers did something
                            // noteworthy to get themselves in the achievement log.  So we leave them
                            // to clutter up the save file.
                            //HighLogic.CurrentGame.CrewRoster.Remove(passenger);
                            passengers.Remove(passenger);
                        }
                    }
                }

            }

            // Vessel with crew
            foreach (ProtoCrewMember crewMember in v.GetVesselCrew())
            {
                // Find this crew member in our data and remove them
                ProtoCrewMember passenger = passengers.Where(pcm => pcm == crewMember).FirstOrDefault();
                if (passenger != null)
                {
                    HighLogic.CurrentGame.CrewRoster.Remove(passenger);
                    passengers.Remove(passenger);
                }
            }
        }

        public void AddPassengersToActiveVessel()
        {
            LoggingUtil.LogVerbose(this, "AddPassengersToActiveVessel");

            // Check the active vessel
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || !HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            foreach (ProtoCrewMember crewMember in passengers)
            {
                // Find a seat for the crew
                Part part = v.parts.Find(p => p.protoModuleCrew.Count < p.CrewCapacity);

                // Add the crew member
                bool success = false;
                if (part != null)
                {
                    // Add them to the part
                    success = part.AddCrewmemberAt(crewMember, part.protoModuleCrew.Count);
                    if (success)
                    {
                        GameEvents.onCrewBoardVessel.Fire(new GameEvents.FromToAction<Part, Part>(part, part));
                        GameEvents.onCrewTransferred.Fire(new GameEvents.HostedFromToAction<ProtoCrewMember, Part>(crewMember, part, part));
                    }
                }

                if (!success)
                {
                    LoggingUtil.LogError(this, "Unable to add crew to vessel named '" + v.name + "'.  Perhaps there's no room?");
                    break;
                }
            }

            // This will force the crew members to appear
            v.SpawnCrew();

            // Update the parameters and force a re-check to update their state
            passengersLoaded = true;
            onPassengersLoaded.Fire();
        }

        protected override void OnAccepted()
        {
            for (int i = 0; i < count; i++)
            {
                // Create the ProtoCrewMember
                ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);

                if (passengerNames.Count > i)
                {
                    crewMember.name = passengerNames[i];
                }
                passengers.Add(crewMember);
            }
            passengerNames.Clear();
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("count", count);
            node.AddValue("passengersLoaded", passengersLoaded);

            foreach (string passenger in passengerNames)
            {
                node.AddValue("potentialPassenger", passenger);
            }

            foreach (ProtoCrewMember passenger in passengers)
            {
                node.AddValue("passenger", passenger.name);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            count = Convert.ToInt32(node.GetValue("count"));
            passengers = ConfigNodeUtil.ParseValue<List<ProtoCrewMember>>(node, "passenger", new List<ProtoCrewMember>());
            passengerNames = ConfigNodeUtil.ParseValue<List<string>>(node, "potentialPassenger", new List<string>());
            passengersLoaded = ConfigNodeUtil.ParseValue<bool>(node, "passengersLoaded");
        }

        protected override void OnCancelled()
        {
            RemoveKerbals();
        }

        protected override void OnDeadlineExpired()
        {
            RemoveKerbals();
        }

        protected override void OnDeclined()
        {
            RemoveKerbals();
        }

        protected override void OnGenerateFailed()
        {
            RemoveKerbals();
        }

        protected override void OnOfferExpired()
        {
            RemoveKerbals();
        }

        protected override void OnWithdrawn()
        {
            RemoveKerbals();
        }

        private void RemoveKerbals()
        {
            foreach (ProtoCrewMember kerbal in passengers)
            {
                HighLogic.CurrentGame.CrewRoster.Remove(kerbal.name);
            }
            passengers.Clear();
        }

        public ProtoCrewMember GetKerbal(int index)
        {
            if (index < 0 || index >= passengers.Count)
            {
                throw new Exception("ContractConfigurator: index " + index +
                    " is out of range for number of passengers spawned (" + passengers.Count + ").");
            }

            return passengers[index];
        }
    }
}
