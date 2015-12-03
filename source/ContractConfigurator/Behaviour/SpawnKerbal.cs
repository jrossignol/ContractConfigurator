using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Class for spawning a Kerbal.
    /// </summary>
    public class SpawnKerbal : ContractBehaviour, IHasKerbalBehaviour
    {
        private class KerbalData
        {
            public Kerbal kerbal = new Kerbal();
            public CelestialBody body = null;
            public Orbit orbit = null;
            public double latitude = 0.0;
            public double longitude = 0.0;
            public double? altitude = 0.0;
            public bool landed = false;
            public bool owned = false;
            public bool addToRoster = true;
            public PQSCity pqsCity = null;
            public Vector3d pqsOffset;
            public float heading;

            public KerbalData() { }
            public KerbalData(KerbalData k)
            {
                kerbal = new Kerbal(k.kerbal);
                body = k.body;
                orbit = k.orbit;
                latitude = k.latitude;
                longitude = k.longitude;
                altitude = k.altitude;
                landed = k.landed;
                owned = k.owned;
                addToRoster = k.addToRoster;
                pqsCity = k.pqsCity;
                pqsOffset = k.pqsOffset;
                heading = k.heading;
            }
        }
        private List<KerbalData> kerbals = new List<KerbalData>();
        private bool initialized = false;

        public int KerbalCount { get { return kerbals.Count; } }

        public SpawnKerbal() {}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="orig"></param>
        public SpawnKerbal(SpawnKerbal orig)
        {
            foreach (KerbalData kerbal in orig.kerbals)
            {
                kerbals.Add(new KerbalData(kerbal));
            }

            if (orig.initialized)
            {
                foreach (KerbalData kd in orig.kerbals)
                {
                    kd.kerbal._pcm = null;
                }
                orig.initialized = false;
            }
            else
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (!initialized)
            {
                LoggingUtil.LogVerbose(this, "Initializing SpawnKerbal.");

                // Update the position information
                foreach (KerbalData kd in kerbals)
                {
                    LoggingUtil.LogVerbose(this, "Positioning kerbal " + kd.kerbal.name);

                    // Generate PQS city coordinates
                    if (kd.pqsCity != null)
                    {
                        LoggingUtil.LogVerbose(this, "Generating coordinates from PQS city for Kerbal " + kd.kerbal.name);

                        // Translate by the PQS offset (inverse transform of coordinate system)
                        Vector3d position = kd.pqsCity.transform.position;
                        Vector3d v = kd.pqsOffset;
                        Vector3d i = kd.pqsCity.transform.right;
                        Vector3d j = kd.pqsCity.transform.forward;
                        Vector3d k = kd.pqsCity.transform.up;
                        Vector3d offsetPos = new Vector3d(
                            (j.y * k.z - j.z * k.y) * v.x + (i.z * k.y - i.y * k.z) * v.y + (i.y * j.z - i.z * j.y) * v.z,
                            (j.z * k.x - j.x * k.z) * v.x + (i.x * k.z - i.z * k.x) * v.y + (i.z * j.x - i.x * j.z) * v.z,
                            (j.x * k.y - j.y * k.x) * v.x + (i.y * k.x - i.x * k.y) * v.y + (i.x * j.y - i.y * j.x) * v.z
                        );
                        offsetPos *= (i.x * j.y * k.z) + (i.y * j.z * k.x) + (i.z * j.x * k.y) - (i.z * j.y * k.x) - (i.y * j.x * k.z) - (i.x * j.z * k.y);
                        kd.latitude = kd.body.GetLatitude(position + offsetPos);
                        kd.longitude = kd.body.GetLongitude(position + offsetPos);
                    }
                }

                initialized = true;
                LoggingUtil.LogVerbose(this, "SpawnKerbal initialized.");
            }
        }

        public void Uninitialize()
        {
            if (initialized)
            {
                RemoveKerbals();
                initialized = false;
            }
        }
        
        public static SpawnKerbal Create(ConfigNode configNode, SpawnKerbalFactory factory)
        {
            SpawnKerbal spawnKerbal = new SpawnKerbal();

            bool valid = true;
            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, "KERBAL"))
            {
                DataNode dataNode = new DataNode("KERBAL_" + index++, factory.dataNode, factory);
                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);

                    KerbalData kd = new KerbalData();

                    // Use an expression to default - then it'll work for dynamic contracts
                    if (!child.HasValue("targetBody"))
                    {
                        child.AddValue("targetBody", "@/targetBody");
                    }
                    valid &= ConfigNodeUtil.ParseValue<CelestialBody>(child, "targetBody", x => kd.body = x, factory);

                    // Get landed stuff
                    if (child.HasValue("lat") && child.HasValue("lon") || child.HasValue("pqsCity"))
                    {
                        kd.landed = true;
                        if (child.HasValue("pqsCity"))
                        {
                            string pqsCityStr = null;
                            valid &= ConfigNodeUtil.ParseValue<string>(child, "pqsCity", x => pqsCityStr = x, factory);
                            if (pqsCityStr != null)
                            {
                                try
                                {
                                    kd.pqsCity = kd.body.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == pqsCityStr).First();
                                }
                                catch (Exception e)
                                {
                                    LoggingUtil.LogError(typeof(WaypointGenerator), "Couldn't load PQSCity with name '" + pqsCityStr + "'");
                                    LoggingUtil.LogException(e);
                                    valid = false;
                                }
                            }
                            valid &= ConfigNodeUtil.ParseValue<Vector3d>(child, "pqsOffset", x => kd.pqsOffset = x, factory, new Vector3d());

                            // Don't expect these to load anything, but do it to mark as initialized
                            valid &= ConfigNodeUtil.ParseValue<double>(child, "lat", x => kd.latitude = x, factory, 0.0);
                            valid &= ConfigNodeUtil.ParseValue<double>(child, "lon", x => kd.longitude = x, factory, 0.0);
                        }
                        else
                        {
                            valid &= ConfigNodeUtil.ParseValue<double>(child, "lat", x => kd.latitude = x, factory);
                            valid &= ConfigNodeUtil.ParseValue<double>(child, "lon", x => kd.longitude = x, factory);
                        }

                        valid &= ConfigNodeUtil.ParseValue<float>(child, "heading", x => kd.heading = x, factory, 0.0f);
                    }
                    // Get orbit
                    else if (child.HasNode("ORBIT"))
                    {
                        // Don't expect these to load anything, but do it to mark as initialized
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lat", x => kd.latitude = x, factory, 0.0);
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lon", x => kd.longitude = x, factory, 0.0);

                        valid &= ConfigNodeUtil.ParseValue<Orbit>(child, "ORBIT", x => kd.orbit = x, factory);
                    }
                    else
                    {
                        // Will error
                        valid &= ConfigNodeUtil.ValidateMandatoryChild(child, "ORBIT", factory);
                    }

                    valid &= ConfigNodeUtil.ParseValue<double?>(child, "alt", x => kd.altitude = x, factory, (double?)null);

                    if (child.HasValue("kerbal"))
                    {
                        valid &= ConfigNodeUtil.ParseValue<Kerbal>(child, "kerbal", x => kd.kerbal = x, factory);
                    }
                    else
                    {
                        // Default gender
                        if (!child.HasValue("gender"))
                        {
                            child.AddValue("gender", "Random()");
                        }
                        valid &= ConfigNodeUtil.ParseValue<ProtoCrewMember.Gender>(child, "gender", x => kd.kerbal.gender = x, factory);

                        // Default name
                        if (!child.HasValue("name"))
                        {
                            child.AddValue("name", "RandomKerbalName(@gender)");
                        }
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "name", x => { kd.kerbal.name = x; if (kd.kerbal.pcm != null) kd.kerbal.pcm.name = x; },
                            factory);
                    }

                    // Get additional stuff
                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "owned", x => kd.owned = x, factory, false);
                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "addToRoster", x => kd.addToRoster = x, factory, true);
                    valid &= ConfigNodeUtil.ParseValue<ProtoCrewMember.KerbalType>(child, "kerbalType", x => kd.kerbal.kerbalType = x, factory, ProtoCrewMember.KerbalType.Unowned);

                    // Check for unexpected values
                    valid &= ConfigNodeUtil.ValidateUnexpectedValues(child, factory);

                    // Add to the list
                    spawnKerbal.kerbals.Add(kd);
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(factory.dataNode);
                }
            }

            return valid ? spawnKerbal : null;
        }

        protected override void OnOffered()
        {
        }

        protected override void OnAccepted()
        {
            // Actually spawn the kerbals in the game world!
            foreach (KerbalData kd in kerbals)
            {
                LoggingUtil.LogVerbose(this, "Spawning a Kerbal named " + kd.kerbal.name);

                // Generate the ProtoCrewMember
                kd.kerbal.GenerateKerbal();

                if (kd.altitude == null)
                {
                    kd.altitude = LocationUtil.TerrainHeight(kd.latitude, kd.longitude, kd.body);
                }

                // Set additional info for landed kerbals
                if (kd.landed)
                {
                    Vector3d pos = kd.body.GetWorldSurfacePosition(kd.latitude, kd.longitude, kd.altitude.Value);

                    kd.orbit = new Orbit(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, kd.body);
                    kd.orbit.UpdateFromStateVectors(pos, kd.body.getRFrmVel(pos), kd.body, Planetarium.GetUniversalTime());
                    LoggingUtil.LogVerbose(typeof(SpawnKerbal), "kerbal generated, orbit = " + kd.orbit);
                }
                else
                {
                    // Update the reference body in the orbit
                    kd.orbit.referenceBody = kd.body;
                }

                uint flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);

                // Create crew member array
                ProtoCrewMember[] crewArray = new ProtoCrewMember[1];
                crewArray[0] = kd.kerbal.pcm;

                // Create part nodes
                ConfigNode[] partNodes = new ConfigNode[1];
                partNodes[0] = ProtoVessel.CreatePartNode(kd.kerbal.gender == ProtoCrewMember.Gender.Male ? "kerbalEVA" : "kerbalEVAfemale",
                    flightId, crewArray);

                // Create additional nodes
                ConfigNode[] additionalNodes = new ConfigNode[1];
                DiscoveryLevels discoveryLevel = kd.owned ? DiscoveryLevels.Owned : DiscoveryLevels.Unowned;
                additionalNodes[0] = ProtoVessel.CreateDiscoveryNode(discoveryLevel, UntrackedObjectClass.A, contract.TimeDeadline, contract.TimeDeadline);

                // Create the config node representation of the ProtoVessel
                ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode(kd.kerbal.name, VesselType.EVA, kd.orbit, 0, partNodes, additionalNodes);

                // Additional seetings for a landed Kerbal
                if (kd.landed)
                {
                    bool splashed = kd.altitude.Value < 0.001;

                    // Add a bit of height for landed kerbals
                    if (!splashed)
                    {
                        kd.altitude += 0.2;
                    }

                    // Figure out the appropriate rotation
                    Vector3d norm = kd.body.GetRelSurfaceNVector(kd.latitude, kd.longitude);
                    Quaternion normal = Quaternion.LookRotation(new Vector3((float)norm.x, (float)norm.y, (float)norm.z));
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                    rotation = rotation * Quaternion.AngleAxis(kd.heading + 180, Vector3.up);

                    // Create the config node representation of the ProtoVessel
                    protoVesselNode.SetValue("sit", (splashed ? Vessel.Situations.SPLASHED : Vessel.Situations.LANDED).ToString());
                    protoVesselNode.SetValue("landed", (!splashed).ToString());
                    protoVesselNode.SetValue("splashed", splashed.ToString());
                    protoVesselNode.SetValue("lat", kd.latitude.ToString());
                    protoVesselNode.SetValue("lon", kd.longitude.ToString());
                    protoVesselNode.SetValue("alt", kd.altitude.ToString());
                    protoVesselNode.SetValue("rot", KSPUtil.WriteQuaternion(normal * rotation));

                    // Set the normal vector relative to the surface
                    Vector3 nrm = (rotation * Vector3.forward);
                    protoVesselNode.SetValue("nrm", nrm.x + "," + nrm.y + "," + nrm.z);
                }

                // Add vessel to the game
                HighLogic.CurrentGame.AddVessel(protoVesselNode);
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);

            foreach (KerbalData kd in kerbals)
            {
                ConfigNode child = new ConfigNode("KERBAL_DETAIL");

                kd.kerbal.Save(child);
                child.AddValue("body", kd.body.name);
                child.AddValue("lat", kd.latitude);
                child.AddValue("lon", kd.longitude);
                if (kd.altitude != null)
                {
                    child.AddValue("alt", kd.altitude);
                }
                child.AddValue("landed", kd.landed);
                child.AddValue("owned", kd.owned);
                child.AddValue("addToRoster", kd.addToRoster);

                if (kd.orbit != null)
                {
                    ConfigNode orbitNode = new ConfigNode("ORBIT");
                    new OrbitSnapshot(kd.orbit).Save(orbitNode);
                    child.AddNode(orbitNode);
                }

                configNode.AddNode(child);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (ConfigNode child in configNode.GetNodes("KERBAL_DETAIL"))
            {
                // Read all the orbit data
                KerbalData kd = new KerbalData();
                kd.body = ConfigNodeUtil.ParseValue<CelestialBody>(child, "body");
                kd.latitude = ConfigNodeUtil.ParseValue<double>(child, "lat");
                kd.longitude = ConfigNodeUtil.ParseValue<double>(child, "lon");
                kd.altitude = ConfigNodeUtil.ParseValue<double?>(child, "alt", (double?)null);
                kd.landed = ConfigNodeUtil.ParseValue<bool>(child, "landed");
                kd.owned = ConfigNodeUtil.ParseValue<bool>(child, "owned");
                kd.addToRoster = ConfigNodeUtil.ParseValue<bool>(child, "addToRoster");

                if (child.HasNode("ORBIT"))
                {
                    kd.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();
                }

                // Load the kerbal
                kd.kerbal = Kerbal.Load(child);
                kerbals.Add(kd);
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
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
                        // Find this crew member in our data
                        foreach (KerbalData kd in kerbals)
                        {
                            if (kd.kerbal.name == name && kd.addToRoster)
                            {
                                // Add them to the roster
                                kd.kerbal.pcm.type = ProtoCrewMember.KerbalType.Crew;
                            }
                        }
                    }
                }

            }

            // Vessel with crew
            foreach (ProtoCrewMember crewMember in v.GetVesselCrew())
            {
                foreach (KerbalData kd in kerbals)
                {
                    if (kd.kerbal.pcm == crewMember && kd.addToRoster)
                    {
                        // Add them to the roster
                        crewMember.type = ProtoCrewMember.KerbalType.Crew;
                    }
                }
            }
        }

        protected override void OnCompleted()
        {
            RemoveKerbals(true);
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

        private void RemoveKerbals(bool onlyUnowned = false)
        {
            LoggingUtil.LogVerbose(this, "Removing kerbals, onlyUnowned = " + onlyUnowned);
            foreach (KerbalData kerbal in kerbals)
            {
                if (!kerbal.addToRoster || !onlyUnowned)
                {
                    LoggingUtil.LogVerbose(this, "    Removing " + kerbal.kerbal.name + "...");
                    Vessel vessel = FlightGlobals.Vessels.Where(v => v.GetVesselCrew().Contains(kerbal.kerbal.pcm)).FirstOrDefault();
                    if (vessel != null)
                    {
                        // If it's an EVA make them disappear...
                        if (vessel.isEVA)
                        {
                            FlightGlobals.Vessels.Remove(vessel);
                        }
                        else
                        {
                            foreach (Part p in vessel.parts)
                            {
                                if (p.protoModuleCrew.Contains(kerbal.kerbal.pcm))
                                {
                                    p.RemoveCrewmember(kerbal.kerbal.pcm);
                                    break;
                                }
                            }
                        }
                    }

                    if (!onlyUnowned)
                    {
                        // Remove the kerbal from the roster
                        HighLogic.CurrentGame.CrewRoster.Remove(kerbal.kerbal.name);
                        kerbal.kerbal._pcm = null;
                    }
                }
            }
            kerbals.Clear();
        }

        public Kerbal GetKerbal(int index)
        {
            if (index < 0 || index >= kerbals.Count)
            {
                throw new Exception("ContractConfigurator: index " + index +
                    " is out of range for number of Kerbals spawned (" + kerbals.Count + ").");
            }

            return kerbals[index].kerbal;
        }

        public IEnumerable<Kerbal> Kerbals()
        {
            foreach (KerbalData kd in kerbals)
            {
                yield return kd.kerbal;
            }
        }
    }
}
