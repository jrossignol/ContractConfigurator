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
    /// Parameter for checking whether a vessel has space for passengers.
    /// </summary>
    public class HasPassengers : VesselParameter
    {
        private class PassengerLoader : MonoBehaviour
        {
            private string contractTitle = "";
            private int passengerCount = 0;
            private HasPassengers parameterReference = null;
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

            public void Show(HasPassengers parameterReference, string contractTitle, int passengerCount)
            {
                visible = true;
                this.parameterReference = parameterReference;
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
                    parameterReference.AddPassengersToActiveVessel();
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

        protected string title { get; set; }
        protected int count { get; set; }

        private List<ProtoCrewMember> passengers = new List<ProtoCrewMember>();

        public HasPassengers()
            : base()
        {
        }

        public HasPassengers(string title, int minPassengers = 1)
            : base()
        {
            this.count = minPassengers;
            this.title = title;

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                if (passengers.Count == 0)
                {
                    output = "Load " + count + " passenger" + (count > 1 ? "s" : "") + " while on the launchpad/runway";
                }
                else if (state == ParameterState.Complete)
                {
                    output = "Passengers: " + count;
                }
                else
                {
                    output = "Passengers";
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected void CreateDelegates()
        {
            // Filter for celestial bodies
            if (passengers.Count > 0)
            {
                foreach (ProtoCrewMember passenger in passengers)
                {
                    AddParameter(new ParameterDelegate<Vessel>("On Board: " + passenger.name,
                        v => v.GetVesselCrew().Contains(passenger), ParameterDelegateMatchType.VALIDATE_ALL));
                }
            }
        }
        
        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("count", count);

            foreach (ProtoCrewMember passenger in passengers)
            {
                node.AddValue("passenger", passenger.name);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            count = Convert.ToInt32(node.GetValue("count"));
            passengers = ConfigNodeUtil.ParseValue<List<ProtoCrewMember>>(node, "passenger", new List<ProtoCrewMember>());

            ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
            CreateDelegates();
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(OnCrewTransferred));
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected override void OnFlightReady()
        {
            base.OnFlightReady();

            // Checks for dispalying the dialog box
            Vessel v = FlightGlobals.ActiveVessel;
            if (v != null && v.situation == Vessel.Situations.PRELAUNCH &&
                passengers.Count == 0 && 
                v.GetCrewCapacity() - v.GetCrewCount() >= count)
            {
                PassengerLoader loader = MapView.MapCamera.gameObject.GetComponent<PassengerLoader>();
                if (loader == null)
                {
                    LoggingUtil.LogVerbose(this, "Adding PassengerLoader");
                    loader = MapView.MapCamera.gameObject.AddComponent<PassengerLoader>();
                }

                loader.Show(this, Root.Title, count);
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
                            HighLogic.CurrentGame.CrewRoster.Remove(passenger);
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
        
        protected override void OnPartAttach(GameEvents.HostTargetAction<Part, Part> e)
        {
            base.OnPartAttach(e);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected override void OnVesselCreate(Vessel vessel)
        {
            base.OnVesselCreate(vessel);
            CheckVessel(vessel);
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        protected void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> a)
        {
            // Check both, as the Kerbal/ship swap spots depending on whether the vessel is
            // incoming or outgoing
            CheckVessel(a.from.vessel);
            CheckVessel(a.to.vessel);
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

            for (int i = 0; i < count; i++)
            {
                // Find a seat for the crew
                Part part = v.parts.Find(p => p.protoModuleCrew.Count < p.CrewCapacity);

                // Add the crew member
                bool success = false;
                if (part != null)
                {
                    // Create the ProtoCrewMember
                    ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
                    passengers.Add(crewMember);

                    // Add them to the part
                    success = part.AddCrewmemberAt(crewMember, part.protoModuleCrew.Count);
                    if (success)
                    {
                        GameEvents.onCrewBoardVessel.Fire(new GameEvents.FromToAction<Part,Part>(part, part));
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
            CreateDelegates();
            CheckVessel(v);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check.</param>
        /// <returns>Whether the vessel meets the conditions.</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            // No passengers loaded
            if (passengers.Count == 0)
            {
                return false;
            }

            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
        }
    }
}
