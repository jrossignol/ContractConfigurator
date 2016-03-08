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
    public class SpawnPassengers : ContractBehaviour, IHasKerbalBehaviour, IKerbalNameStorage
    {
        public static EventVoid onPassengersLoaded = new EventVoid("onPassengersLoaded");

        private class PassengerLoader : MonoBehaviour
        {
            private class PassengerDetail
            {
                public string contractTitle;
                public int passengerCount;
                public List<SpawnPassengers> behaviourList;
                public bool selected = false;

                public PassengerDetail(string contractTitle, int passengerCount, List<SpawnPassengers> behaviourList)
                {
                    this.contractTitle = contractTitle;
                    this.passengerCount = passengerCount;
                    this.behaviourList = behaviourList;
                }
            }
            List<PassengerDetail> passengerDetails = new List<PassengerDetail>();
            int totalPassengers;
            int selectedPassengers;

            private static bool stylesSetup = false;
            private static GUIStyle redLabel;
            private static GUIStyle disabledButton;

            private bool uiHidden = false;
            private bool visible = false;
            private Rect windowPos = new Rect((Screen.width - 480) / 2, (Screen.height - 600) / 2, 480, 600);

            private double stateChangeTime = double.MaxValue;

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

            public void Show()
            {
                if (visible)
                {
                    return;
                }

                visible = true;
                passengerDetails.Clear();
                selectedPassengers = totalPassengers = 0;
                int capacity = FlightGlobals.ActiveVessel.GetCrewCapacity() - FlightGlobals.ActiveVessel.GetCrewCount();

                foreach (ConfiguredContract contract in ContractSystem.Instance.GetCurrentActiveContracts<ConfiguredContract>())
                {
                    string contractTitle = contract.Title;
                    int passengerCount = 0;
                    List<SpawnPassengers> passengerList = new List<SpawnPassengers>();
                    foreach (SpawnPassengers sp in contract.Behaviours.Where(x => x.GetType() == typeof(SpawnPassengers)))
                    {
                        int count = sp.passengers.Where(pair => !pair.Value && pair.Key.rosterStatus == ProtoCrewMember.RosterStatus.Available).Count();
                        if (count != 0)
                        {
                            passengerCount += count;
                            passengerList.Add(sp);
                        }
                    }

                    if (passengerCount > 0)
                    {
                        totalPassengers += passengerCount;
                        PassengerDetail pd = new PassengerDetail(contractTitle, passengerCount, passengerList);

                        if (capacity >= passengerCount)
                        {
                            pd.selected = true;
                            selectedPassengers += passengerCount;
                            capacity -= passengerCount;
                        }

                        passengerDetails.Add(pd);
                    }
                }
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
                    if (FlightGlobals.ActiveVessel == null || stateChangeTime < Time.fixedTime)
                    {
                        visible = false;
                        stateChangeTime = double.MaxValue;
                    }
                    else if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.PRELAUNCH &&
                        FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED)
                    {
                        stateChangeTime = Time.fixedTime + 2.5;
                    }
                    else
                    {
                        stateChangeTime = double.MaxValue;
                    }
                    

                    GUI.skin = HighLogic.Skin;
                    if (!stylesSetup)
                    {
                        stylesSetup = true;

                        redLabel = new GUIStyle(GUI.skin.label);
                        redLabel.normal.textColor = Color.red;
                        disabledButton = new GUIStyle(GUI.skin.button);
                        disabledButton.normal.textColor = new Color(0.2f, 0.2f, 0.2f);
                        disabledButton.focused = disabledButton.normal;
                        disabledButton.hover = disabledButton.normal;
                    }

                    windowPos = GUILayout.Window(
                        GetType().FullName.GetHashCode(),
                        windowPos,
                        PassengerDialog,
                        "Load Passengers?",
                        GUILayout.Width(480),
                        GUILayout.Height(120));
                }
            }

            void PassengerDialog(int windowID)
            {
                Vessel v = FlightGlobals.ActiveVessel;
                int emptySeats = v.GetCrewCapacity() - v.GetCrewCount();

                GUILayout.BeginVertical();

                GUILayout.Label("One or more contracts require passengers to be loaded.  Would you like to load them onto this vessel?");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Passengers to load:");
                GUILayout.Label(selectedPassengers.ToString(), (selectedPassengers > emptySeats ? redLabel : GUI.skin.label));
                GUILayout.EndHorizontal();
                GUILayout.Label("Empty seats on vessel: " + emptySeats);

                GUILayout.BeginVertical(GUI.skin.box);

                int count = 0;
                selectedPassengers = 0;
                foreach (PassengerDetail pd in passengerDetails)
                {
                    pd.selected = GUILayout.Toggle(pd.selected, pd.passengerCount + " passenger" + (pd.passengerCount > 1 ? "s: " : ": ") + pd.contractTitle);
                    if (pd.selected)
                    {
                        count += pd.passengerCount;
                        selectedPassengers += pd.passengerCount;
                    }
                }

                GUILayout.EndVertical();

                if (GUILayout.Button("Load passengers", (count > emptySeats ? disabledButton : GUI.skin.button)) && count <= emptySeats)
                {
                    foreach (PassengerDetail pd in passengerDetails)
                    {
                        if (pd.selected)
                        {
                            foreach (SpawnPassengers sp in pd.behaviourList)
                            {
                                sp.AddPassengersToActiveVessel();
                            }
                        }
                    }
                    visible = false;
                    Destroy(this);
                }

                if (GUILayout.Button("No passengers"))
                {
                    visible = false;
                    Destroy(this);
                }

                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        protected int count;
        protected List<Kerbal> kerbals = new List<Kerbal>();
        protected bool removePassengers;

        private Dictionary<ProtoCrewMember, bool> passengers = new Dictionary<ProtoCrewMember, bool>();

        public int KerbalCount { get { return passengers.Count; } }

        public SpawnPassengers() {}

        public SpawnPassengers(List<Kerbal> kerbals, int minPassengers, bool removePassengers)
        {
            this.kerbals = kerbals;
            this.count = kerbals.Count != 0 ? kerbals.Count : minPassengers;
            this.removePassengers = removePassengers;
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
            // Checks for displaying the dialog box
            Vessel v = FlightGlobals.ActiveVessel;
            IEnumerable<ProtoCrewMember> vesselCrew = v.GetVesselCrew();
            if (v != null && v.situation == Vessel.Situations.PRELAUNCH &&
                v.mainBody.isHomeWorld &&
                passengers.Where(pair => !pair.Value && pair.Key.rosterStatus == ProtoCrewMember.RosterStatus.Available).Any() &&
                v.GetCrewCapacity() - v.GetCrewCount() >= count)
            {
                PassengerLoader loader = MapView.MapCamera.gameObject.GetComponent<PassengerLoader>();
                if (loader == null)
                {
                    LoggingUtil.LogVerbose(this, "Adding PassengerLoader");
                    loader = MapView.MapCamera.gameObject.AddComponent<PassengerLoader>();
                }

                loader.Show();
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
                        ProtoCrewMember passenger = passengers.Keys.Where(pcm => pcm.name == name).FirstOrDefault();
                        if (passenger != null)
                        {
                            passengers[passenger] = false;
                        }
                    }
                }

            }

            // Vessel with crew
            foreach (ProtoCrewMember crewMember in v.GetVesselCrew())
            {
                // Find this crew member in our data and remove them
                ProtoCrewMember passenger = passengers.Keys.Where(pcm => pcm == crewMember).FirstOrDefault();
                if (passenger != null)
                {
                    passengers[passenger] = false;
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

            foreach (ProtoCrewMember crewMember in passengers.Keys.Where(pcm => pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available).ToList())
            {
                // Find a seat for the crew
                Part part = v.parts.Find(p => p.protoModuleCrew.Count < p.CrewCapacity);

                // Add the crew member
                bool success = false;
                if (part != null)
                {
                    // Add them to the part
                    success = part.AddCrewmember(crewMember);
                    if (success)
                    {
                        passengers[crewMember] = true;
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
            onPassengersLoaded.Fire();
        }

        protected override void OnAccepted()
        {
            // Create all the passengers
            for (int i = 0; i < count; i++)
            {
                // Create the ProtoCrewMember
                ProtoCrewMember crewMember = null;

                // Try to get existing passenger
                if (kerbals.Count > i)
                {
                    crewMember = HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(pcm => pcm.name == kerbals[i].name).FirstOrDefault();
                    if (crewMember != null)
                    {
                        crewMember.hasToured = false;
                    }
                }
                if (crewMember == null)
                {
                    // Generate the ProtoCrewMember
                    Kerbal kerbal = (i < kerbals.Count()) ? kerbals.ElementAt(i) : new Kerbal();
                    if (i >= kerbals.Count())
                    {
                        kerbal.kerbalType = ProtoCrewMember.KerbalType.Tourist;
                    }
                    kerbal.GenerateKerbal();
                    crewMember = kerbal.pcm;
                }

                passengers[crewMember] = false;
            }

            kerbals.Clear();
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("count", count);
            if (!removePassengers)
            {
                node.AddValue("removePassengers", false);
            }

            foreach (Kerbal kerbal in kerbals)
            {
                ConfigNode kerbalNode = new ConfigNode("KERBAL");
                node.AddNode(kerbalNode);

                kerbal.Save(kerbalNode);
            }
            foreach (KeyValuePair<ProtoCrewMember, bool> pair in passengers)
            {
                ConfigNode child = new ConfigNode("PASSENGER_DATA");
                node.AddNode(child);

                child.AddValue("passenger", pair.Key.name);
                child.AddValue("loaded", pair.Value);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            count = Convert.ToInt32(node.GetValue("count"));
            removePassengers = ConfigNodeUtil.ParseValue<bool?>(node, "removePassengers", null) ?? true;

            // Legacy support from Contract Configurator 1.8.3
            if (node.HasValue("potentialPassenger"))
            {
                List<string> passengerNames = ConfigNodeUtil.ParseValue<List<string>>(node, "potentialPassenger", new List<string>());
                ProtoCrewMember.Gender gender = ConfigNodeUtil.ParseValue<ProtoCrewMember.Gender>(node, "gender", Kerbal.RandomGender());
                ProtoCrewMember.KerbalType kerbalType = ConfigNodeUtil.ParseValue<ProtoCrewMember.KerbalType>(node, "kerbalType", ProtoCrewMember.KerbalType.Tourist);
                string experienceTrait = ConfigNodeUtil.ParseValue<string>(node, "experienceTrait", Kerbal.RandomExperienceTrait());

                kerbals = passengerNames.Select(name => new Kerbal(gender, name, experienceTrait)).ToList();

                foreach (Kerbal kerbal in kerbals)
                {
                    kerbal.kerbalType = kerbalType;
                }
            }
            else
            {
                foreach (ConfigNode kerbalNode in node.GetNodes("KERBAL"))
                {
                    kerbals.Add(Kerbal.Load(kerbalNode));
                }
            }

            foreach (ConfigNode child in node.GetNodes("PASSENGER_DATA"))
            {
                ProtoCrewMember crew = ConfigNodeUtil.ParseValue<ProtoCrewMember>(child, "passenger");
                bool loaded = ConfigNodeUtil.ParseValue<bool>(child, "loaded");

                if (crew != null)
                {
                    passengers[crew] = loaded;
                }
            }
        }

        protected override void OnCompleted()
        {
            if (removePassengers)
            {
                RemoveKerbals();
            }
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
            LoggingUtil.LogVerbose(this, "Removing kerbals");
            foreach (ProtoCrewMember kerbal in passengers.Keys)
            {
                Kerbal.RemoveKerbal(kerbal);
            }
            passengers.Clear();
        }

        public Kerbal GetKerbal(int index)
        {
            if (index < 0 || index >= passengers.Count)
            {
                throw new Exception("ContractConfigurator: index " + index +
                    " is out of range for number of passengers spawned (" + passengers.Count + ").");
            }

            int i = 0;
            foreach (ProtoCrewMember pcm in passengers.Keys)
            {
                if (i++ == index)
                {
                    return new Kerbal(pcm);
                }
            }
            return null;
        }

        public IEnumerable<string> KerbalNames()
        {
            foreach (ProtoCrewMember pcm in passengers.Keys)
            {
                yield return pcm.name;
            }
        }
    }
}
