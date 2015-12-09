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
    /// Behaviour class for spawning a vessel.
    /// </summary>
    public class SpawnVessel : ContractBehaviour, IHasKerbalBehaviour, IKerbalNameStorage
    {
        private class CrewData
        {
            public string name = null;
            public ProtoCrewMember.Gender? gender = null;
            public bool addToRoster = true;

            public CrewData() { }
            public CrewData(CrewData cd)
            {
                name = cd.name;
                gender = cd.gender;
                addToRoster = cd.addToRoster;
            }
        }

        private class VesselData
        {
            public string name = null;
            public Guid? id = null;
            public string craftURL = null;
            public AvailablePart craftPart = null;
            public string flagURL = null;
            public VesselType vesselType = VesselType.Ship;
            public CelestialBody body = null;
            public Orbit orbit = null;
            public double latitude = 0.0;
            public double longitude = 0.0;
            public double? altitude = null;
            public float height = 0.0f;
            public bool orbiting = false;
            public bool owned = false;
            public List<CrewData> crew = new List<CrewData>();
            public PQSCity pqsCity = null;
            public Vector3d pqsOffset;
            public float heading;
            public float pitch;
            public float roll;

            public VesselData() { }
            public VesselData(VesselData vd)
            {
                name = vd.name;
                id = vd.id;
                craftURL = vd.craftURL;
                craftPart = vd.craftPart;
                flagURL = vd.flagURL;
                vesselType = vd.vesselType;
                body = vd.body;
                orbit = vd.orbit;
                latitude = vd.latitude;
                longitude = vd.longitude;
                altitude = vd.altitude;
                height = vd.height;
                orbiting = vd.orbiting;
                owned = vd.owned;
                pqsCity = vd.pqsCity;
                pqsOffset = vd.pqsOffset;
                heading = vd.heading;
                pitch = vd.pitch;
                roll = vd.roll;

                foreach (CrewData cd in vd.crew)
                {
                    crew.Add(new CrewData(cd));
                }
            }
        }
        private List<VesselData> vessels = new List<VesselData>();
        private bool vesselsCreated = false;
        private bool deferVesselCreation = false;

        public int KerbalCount
        {
            get
            {
                return vessels.Sum(vd => vd.crew.Count);
            }
        }

        public SpawnVessel() { }

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        /// <param name="orig"></param>
        public SpawnVessel(SpawnVessel orig)
        {
            deferVesselCreation = orig.deferVesselCreation;
            foreach (VesselData vd in orig.vessels)
            {
                vessels.Add(new VesselData(vd));
            }
        }

        public static SpawnVessel Create(ConfigNode configNode, SpawnVesselFactory factory)
        {
            SpawnVessel spawnVessel = new SpawnVessel();

            ConfigNodeUtil.ParseValue<bool>(configNode, "deferVesselCreation", x => spawnVessel.deferVesselCreation = x, factory, false);

            bool valid = true;
            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, "VESSEL"))
            {
                DataNode dataNode = new DataNode("VESSEL_" + index++, factory.dataNode, factory);
                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);

                    VesselData vessel = new VesselData();

                    // Get name
                    if (child.HasValue("name"))
                    {
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "name", x => vessel.name = x, factory);
                    }

                    // Get craft details
                    if (child.HasValue("craftURL"))
                    {
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "craftURL", x => vessel.craftURL = x, factory);
                    }
                    if (child.HasValue("craftPart"))
                    {
                        valid &= ConfigNodeUtil.ParseValue<AvailablePart>(child, "craftPart", x => vessel.craftPart = x, factory);
                    }
                    valid &= ConfigNodeUtil.AtLeastOne(child, new string[] { "craftURL", "craftPart" }, factory);

                    valid &= ConfigNodeUtil.ParseValue<string>(child, "flagURL", x => vessel.flagURL = x, factory, (string)null);
                    valid &= ConfigNodeUtil.ParseValue<VesselType>(child, "vesselType", x => vessel.vesselType = x, factory, VesselType.Ship);

                    // Use an expression to default - then it'll work for dynamic contracts
                    if (!child.HasValue("targetBody"))
                    {
                        child.AddValue("targetBody", "@/targetBody");
                    }
                    valid &= ConfigNodeUtil.ParseValue<CelestialBody>(child, "targetBody", x => vessel.body = x, factory);

                    // Get landed stuff
                    if (child.HasValue("pqsCity"))
                    {
                        string pqsCityStr = null;
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "pqsCity", x => pqsCityStr = x, factory);
                        if (pqsCityStr != null)
                        {
                            try
                            {
                                vessel.pqsCity = vessel.body.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == pqsCityStr).First();
                            }
                            catch (Exception e)
                            {
                                LoggingUtil.LogError(typeof(WaypointGenerator), "Couldn't load PQSCity with name '" + pqsCityStr + "'");
                                LoggingUtil.LogException(e);
                                valid = false;
                            }
                        }
                        valid &= ConfigNodeUtil.ParseValue<Vector3d>(child, "pqsOffset", x => vessel.pqsOffset = x, factory, new Vector3d());

                        // Don't expect these to load anything, but do it to mark as initialized
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lat", x => vessel.latitude = x, factory, 0.0);
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lon", x => vessel.longitude = x, factory, 0.0);

                        // Do load alt and height
                        valid &= ConfigNodeUtil.ParseValue<double?>(child, "alt", x => vessel.altitude = x, factory, (double?)null);
                        valid &= ConfigNodeUtil.ParseValue<float>(child, "height", x => vessel.height = x, factory,
                            !string.IsNullOrEmpty(vessel.craftURL) ? 0.0f : 2.5f);
                        vessel.orbiting = false;

                        // Generate PQS city coordinates
                        LoggingUtil.LogVerbose(factory, "Generating coordinates from PQS city for Vessel " + vessel.name);

                        // Translate by the PQS offset (inverse transform of coordinate system)
                        Vector3d position = vessel.pqsCity.transform.position;
                        Vector3d v = vessel.pqsOffset;
                        Vector3d i = vessel.pqsCity.transform.right;
                        Vector3d j = vessel.pqsCity.transform.forward;
                        Vector3d k = vessel.pqsCity.transform.up;
                        Vector3d offsetPos = new Vector3d(
                            (j.y * k.z - j.z * k.y) * v.x + (i.z * k.y - i.y * k.z) * v.y + (i.y * j.z - i.z * j.y) * v.z,
                            (j.z * k.x - j.x * k.z) * v.x + (i.x * k.z - i.z * k.x) * v.y + (i.z * j.x - i.x * j.z) * v.z,
                            (j.x * k.y - j.y * k.x) * v.x + (i.y * k.x - i.x * k.y) * v.y + (i.x * j.y - i.y * j.x) * v.z
                        );
                        offsetPos *= (i.x * j.y * k.z) + (i.y * j.z * k.x) + (i.z * j.x * k.y) - (i.z * j.y * k.x) - (i.y * j.x * k.z) - (i.x * j.z * k.y);
                        vessel.latitude = vessel.body.GetLatitude(position + offsetPos);
                        vessel.longitude = vessel.body.GetLongitude(position + offsetPos);
                    }
                    else if (child.HasValue("lat") && child.HasValue("lon"))
                    {
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lat", x => vessel.latitude = x, factory);
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lon", x => vessel.longitude = x, factory);
                        valid &= ConfigNodeUtil.ParseValue<double?>(child, "alt", x => vessel.altitude = x, factory, (double?)null);
                        valid &= ConfigNodeUtil.ParseValue<float>(child, "height", x => vessel.height = x, factory,
                            !string.IsNullOrEmpty(vessel.craftURL) ? 0.0f : 2.5f);
                        vessel.orbiting = false;
                    }
                    // Get orbit
                    else
                    {
                        valid &= ConfigNodeUtil.ParseValue<Orbit>(child, "ORBIT", x => vessel.orbit = x, factory);
                        vessel.orbiting = true;
                    }

                    valid &= ConfigNodeUtil.ParseValue<float>(child, "heading", x => vessel.heading = x, factory, 0.0f);
                    valid &= ConfigNodeUtil.ParseValue<float>(child, "pitch", x => vessel.pitch = x, factory, 0.0f);
                    valid &= ConfigNodeUtil.ParseValue<float>(child, "roll", x => vessel.roll = x, factory, 0.0f);

                    // Get additional flags
                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "owned", x => vessel.owned = x, factory, false);

                    // Handle the CREW nodes
                    foreach (ConfigNode crewNode in ConfigNodeUtil.GetChildNodes(child, "CREW"))
                    {
                        int count = 1;
                        valid &= ConfigNodeUtil.ParseValue<int>(crewNode, "count", x => count = x, factory, 1);
                        for (int i = 0; i < count; i++)
                        {
                            CrewData cd = new CrewData();

                            // Read crew details
                            valid &= ConfigNodeUtil.ParseValue<string>(crewNode, "name", x => cd.name = x, factory, (string)null);
                            valid &= ConfigNodeUtil.ParseValue<bool>(crewNode, "addToRoster", x => cd.addToRoster = x, factory, true);

                            // Check for unexpected values
                            valid &= ConfigNodeUtil.ValidateUnexpectedValues(crewNode, factory);

                            // Add the record
                            vessel.crew.Add(cd);
                        }
                    }

                    // Check for unexpected values
                    valid &= ConfigNodeUtil.ValidateUnexpectedValues(child, factory);

                    // Add to the list
                    spawnVessel.vessels.Add(vessel);
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(factory.dataNode);
                }
            }

            if (!configNode.HasNode("VESSEL"))
            {
                valid = false;
                LoggingUtil.LogError(factory, "SpawnVessel requires at least one VESSEL node.");
            }

            return valid ? spawnVessel : null;
        }

        protected void CreateVessels()
        {
            if (vesselsCreated)
            {
                return;
            }

            String gameDataDir = KSPUtil.ApplicationRootPath;
            gameDataDir = gameDataDir.Replace("\\", "/");
            if (!gameDataDir.EndsWith("/"))
            {
                gameDataDir += "/";
            }
            gameDataDir += "GameData";

            // Spawn the vessel in the game world
            foreach (VesselData vesselData in vessels)
            {
                LoggingUtil.LogVerbose(this, "Spawning a vessel named '" + vesselData.name + "'");

                // Set additional info for landed vessels
                bool landed = false;
                if (!vesselData.orbiting)
                {
                    landed = true;
                    if (vesselData.altitude == null)
                    {
                        vesselData.altitude = LocationUtil.TerrainHeight(vesselData.latitude, vesselData.longitude, vesselData.body);
                    }

                    Vector3d pos = vesselData.body.GetWorldSurfacePosition(vesselData.latitude, vesselData.longitude, vesselData.altitude.Value);

                    vesselData.orbit = new Orbit(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, vesselData.body);
                    vesselData.orbit.UpdateFromStateVectors(pos, vesselData.body.getRFrmVel(pos), vesselData.body, Planetarium.GetUniversalTime());
                }
                else
                {
                    vesselData.orbit.referenceBody = vesselData.body;
                }

                ConfigNode[] partNodes;
                UntrackedObjectClass sizeClass;
                ShipConstruct shipConstruct = null;
                if (!string.IsNullOrEmpty(vesselData.craftURL))
                {
                    // Save the current ShipConstruction ship, otherwise the player will see the spawned ship next time they enter the VAB!
                    ConfigNode currentShip = ShipConstruction.ShipConfig;

                    shipConstruct = ShipConstruction.LoadShip(gameDataDir + "/" + vesselData.craftURL);
                    if (shipConstruct == null)
                    {
                        LoggingUtil.LogError(this, "ShipConstruct was null when tried to load '" + vesselData.craftURL +
                            "' (usually this means the file could not be found).");
                        continue;
                    }

                    // Restore ShipConstruction ship
                    ShipConstruction.ShipConfig = currentShip;

                    // Set the name
                    if (string.IsNullOrEmpty(vesselData.name))
                    {
                        vesselData.name = shipConstruct.shipName;
                    }

                    // Set some parameters that need to be at the part level
                    uint missionID = (uint)Guid.NewGuid().GetHashCode();
                    uint launchID = HighLogic.CurrentGame.launchID++;
                    foreach (Part p in shipConstruct.parts)
                    {
                        p.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
                        p.missionID = missionID;
                        p.launchID = launchID;
                        p.flagURL = vesselData.flagURL ?? HighLogic.CurrentGame.flagURL;

                        // Had some issues with this being set to -1 for some ships - can't figure out
                        // why.  End result is the vessel exploding, so let's just set it to a positive
                        // value.
                        p.temperature = 1.0;
                    }

                    foreach (CrewData cd in vesselData.crew)
                    {
                        bool success = false;

                        // Find a seat for the crew
                        Part part = shipConstruct.parts.Find(p => p.protoModuleCrew.Count < p.CrewCapacity);

                        // Add the crew member
                        if (part != null)
                        {
                            // Create the ProtoCrewMember
                            ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
                            if (cd.gender != null)
                            {
                                crewMember.gender = cd.gender.Value;
                            }
                            if (cd.name != null)
                            {
                                crewMember.name = cd.name;
                            }

                            // Add them to the part
                            success = part.AddCrewmemberAt(crewMember, part.protoModuleCrew.Count);
                        }

                        if (!success)
                        {
                            LoggingUtil.LogWarning(this, "Unable to add crew to vessel named '" + vesselData.name + "'.  Perhaps there's no room?");
                            break;
                        }
                    }

                    // Create a dummy ProtoVessel, we will use this to dump the parts to a config node.
                    // We can't use the config nodes from the .craft file, because they are in a
                    // slightly different format than those required for a ProtoVessel (seriously
                    // Squad?!?).
                    ConfigNode empty = new ConfigNode();
                    ProtoVessel dummyProto = new ProtoVessel(empty, null);
                    Vessel dummyVessel = new Vessel();
                    dummyVessel.parts = shipConstruct.parts;
                    dummyProto.vesselRef = dummyVessel;

                    // Create the ProtoPartSnapshot objects and then initialize them
                    foreach (Part p in shipConstruct.parts)
                    {
                        dummyProto.protoPartSnapshots.Add(new ProtoPartSnapshot(p, dummyProto));
                    }
                    foreach (ProtoPartSnapshot p in dummyProto.protoPartSnapshots)
                    {
                        p.storePartRefs();
                    }

                    // Create the ship's parts
                    partNodes = dummyProto.protoPartSnapshots.Select<ProtoPartSnapshot, ConfigNode>(GetNodeForPart).ToArray();

                    // Estimate an object class, numbers are based on the in game description of the
                    // size classes.
                    float size = shipConstruct.shipSize.magnitude / 2.0f;
                    if (size < 4.0f)
                    {
                        sizeClass = UntrackedObjectClass.A;
                    }
                    else if (size < 7.0f)
                    {
                        sizeClass = UntrackedObjectClass.B;
                    }
                    else if (size < 12.0f)
                    {
                        sizeClass = UntrackedObjectClass.C;
                    }
                    else if (size < 18.0f)
                    {
                        sizeClass = UntrackedObjectClass.D;
                    }
                    else
                    {
                        sizeClass = UntrackedObjectClass.E;
                    }
                }
                else
                {
                    // Create crew member array
                    ProtoCrewMember[] crewArray = new ProtoCrewMember[vesselData.crew.Count];
                    int i = 0;
                    foreach (CrewData cd in vesselData.crew)
                    {
                        // Create the ProtoCrewMember
                        ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
                        if (cd.name != null)
                        {
                            crewMember.name = cd.name;
                        }

                        crewArray[i++] = crewMember;
                    }

                    // Create part nodes
                    uint flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
                    partNodes = new ConfigNode[1];
                    partNodes[0] = ProtoVessel.CreatePartNode(vesselData.craftPart.name, flightId, crewArray);

                    // Default the size class
                    sizeClass = UntrackedObjectClass.A;

                    // Set the name
                    if (string.IsNullOrEmpty(vesselData.name))
                    {
                        vesselData.name = vesselData.craftPart.name;
                    }
                }

                // Create additional nodes
                ConfigNode[] additionalNodes = new ConfigNode[1];
                DiscoveryLevels discoveryLevel = vesselData.owned ? DiscoveryLevels.Owned : DiscoveryLevels.Unowned;
                additionalNodes[0] = ProtoVessel.CreateDiscoveryNode(discoveryLevel, sizeClass, contract.TimeDeadline, contract.TimeDeadline);

                // Create the config node representation of the ProtoVessel
                ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode(vesselData.name, vesselData.vesselType, vesselData.orbit, 0, partNodes, additionalNodes);

                // Additional seetings for a landed vessel
                if (!vesselData.orbiting)
                {
                    Vector3d norm = vesselData.body.GetRelSurfaceNVector(vesselData.latitude, vesselData.longitude);
                    
                    double terrainHeight = 0.0;
                    if (vesselData.body.pqsController != null)
                    {
                        terrainHeight = vesselData.body.pqsController.GetSurfaceHeight(norm) - vesselData.body.pqsController.radius;
                    }
                    bool splashed = landed && terrainHeight < 0.001;

                    // Create the config node representation of the ProtoVessel
                    // Note - flying is experimental, and so far doesn't work
                    protoVesselNode.SetValue("sit", (splashed ? Vessel.Situations.SPLASHED : landed ?
                        Vessel.Situations.LANDED : Vessel.Situations.FLYING).ToString());
                    protoVesselNode.SetValue("landed", (landed && !splashed).ToString());
                    protoVesselNode.SetValue("splashed", splashed.ToString());
                    protoVesselNode.SetValue("lat", vesselData.latitude.ToString());
                    protoVesselNode.SetValue("lon", vesselData.longitude.ToString());
                    protoVesselNode.SetValue("alt", vesselData.altitude.ToString());
                    protoVesselNode.SetValue("landedAt", vesselData.body.name);

                    // Figure out the additional height to subtract
                    float lowest = float.MaxValue;
                    if (shipConstruct != null)
                    {
                        foreach (Part p in shipConstruct.parts)
                        {
                            foreach (Collider collider in p.GetComponentsInChildren<Collider>())
                            {
                                if (collider.gameObject.layer != 21 && collider.enabled)
                                {
                                    lowest = Mathf.Min(lowest, collider.bounds.min.y);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (Collider collider in vesselData.craftPart.partPrefab.GetComponentsInChildren<Collider>())
                        {
                            if (collider.gameObject.layer != 21 && collider.enabled)
                            {
                                lowest = Mathf.Min(lowest, collider.bounds.min.y);
                            }
                        }
                    }

                    if (lowest == float.MaxValue)
                    {
                        lowest = 0;
                    }

                    // Figure out the surface height and rotation
                    Quaternion normal = Quaternion.LookRotation(new Vector3((float)norm.x, (float)norm.y, (float)norm.z));
                    Quaternion rotation = Quaternion.identity;
                    float heading = vesselData.heading;
                    if (shipConstruct == null)
                    {
                        rotation = rotation * Quaternion.FromToRotation(Vector3.up, Vector3.back);
                    }
                    else if (shipConstruct.shipFacility == EditorFacility.SPH)
                    {
                        rotation = rotation * Quaternion.FromToRotation(Vector3.forward, -Vector3.forward);
                        heading += 180.0f;
                    }
                    else
                    {
                        rotation = rotation * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
                    }

                    rotation = rotation * Quaternion.AngleAxis(heading, Vector3.back);
                    rotation = rotation * Quaternion.AngleAxis(vesselData.roll, Vector3.down);
                    rotation = rotation * Quaternion.AngleAxis(vesselData.pitch, Vector3.left);

                    // Set the height and rotation
                    if (landed || splashed)
                    {
                        float hgt = (shipConstruct != null ? shipConstruct.parts[0] : vesselData.craftPart.partPrefab).localRoot.attPos0.y - lowest;
                        hgt += vesselData.height;
                        protoVesselNode.SetValue("hgt", hgt.ToString());
                    }
                    protoVesselNode.SetValue("rot", KSPUtil.WriteQuaternion(normal * rotation));

                    // Set the normal vector relative to the surface
                    Vector3 nrm = (rotation * Vector3.forward);
                    protoVesselNode.SetValue("nrm", nrm.x + "," + nrm.y + "," + nrm.z);

                    protoVesselNode.SetValue("prst", false.ToString());
                }

                // Add vessel to the game
                ProtoVessel protoVessel = HighLogic.CurrentGame.AddVessel(protoVesselNode);

                // Store the id for later use
                vesselData.id = protoVessel.vesselRef.id;

                // Associate it so that it can be used in contract parameters
                ContractVesselTracker.Instance.AssociateVessel(vesselData.name, protoVessel.vesselRef);
            }

            vesselsCreated = true;
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("vesselsCreated", vesselsCreated);
            configNode.AddValue("deferVesselCreation", deferVesselCreation);

            foreach (VesselData vd in vessels)
            {
                ConfigNode child = new ConfigNode("VESSEL_DETAIL");

                child.AddValue("name", vd.name);
                if (vd.id != null)
                {
                    child.AddValue("id", vd.id);
                }
                child.AddValue("craftURL", vd.craftURL);
                child.AddValue("flagURL", vd.flagURL);
                child.AddValue("vesselType", vd.vesselType);
                child.AddValue("body", vd.body.name);
                child.AddValue("lat", vd.latitude);
                child.AddValue("lon", vd.longitude);
                if (vd.altitude != null)
                {
                    child.AddValue("alt", vd.altitude);
                }
                child.AddValue("heading", vd.heading);
                child.AddValue("pitch", vd.pitch);
                child.AddValue("roll", vd.roll);
                child.AddValue("orbiting", vd.orbiting);
                child.AddValue("owned", vd.owned);

                if (vd.orbit != null)
                {
                    ConfigNode orbitNode = new ConfigNode("ORBIT");
                    new OrbitSnapshot(vd.orbit).Save(orbitNode);
                    child.AddNode(orbitNode);
                }

                // Add crew data
                foreach (CrewData cd in vd.crew)
                {
                    ConfigNode crewNode = new ConfigNode("CREW");

                    if (!string.IsNullOrEmpty(cd.name))
                    {
                        crewNode.AddValue("name", cd.name);
                    }
                    crewNode.AddValue("addToRoster", cd.addToRoster);

                    child.AddNode(crewNode);
                }

                configNode.AddNode(child);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            vesselsCreated = ConfigNodeUtil.ParseValue<bool>(configNode, "vesselsCreated");
            deferVesselCreation = ConfigNodeUtil.ParseValue<bool?>(configNode, "deferVesselCreation", (bool?)false).Value;

            foreach (ConfigNode child in configNode.GetNodes("VESSEL_DETAIL"))
            {
                // Read all the orbit data
                VesselData vd = new VesselData();
                vd.name = child.GetValue("name");
                vd.id = ConfigNodeUtil.ParseValue<Guid?>(child, "id", (Guid?)null);
                vd.craftURL = child.GetValue("craftURL");
                vd.flagURL = ConfigNodeUtil.ParseValue<string>(child, "flagURL", (string)null);
                vd.vesselType = ConfigNodeUtil.ParseValue<VesselType>(child, "vesselType");
                vd.body = ConfigNodeUtil.ParseValue<CelestialBody>(child, "body");
                vd.latitude = ConfigNodeUtil.ParseValue<double>(child, "lat");
                vd.longitude = ConfigNodeUtil.ParseValue<double>(child, "lon");
                vd.altitude = ConfigNodeUtil.ParseValue<double?>(child, "alt", (double?)null);
                vd.heading = ConfigNodeUtil.ParseValue<float>(child, "heading", 0.0f);
                vd.pitch = ConfigNodeUtil.ParseValue<float>(child, "pitch", 0.0f);
                vd.roll = ConfigNodeUtil.ParseValue<float>(child, "roll", 0.0f);
                vd.orbiting = ConfigNodeUtil.ParseValue<bool?>(child, "orbiting", (bool?)child.HasNode("ORBIT")).Value;
                vd.owned = ConfigNodeUtil.ParseValue<bool>(child, "owned");

                if (child.HasNode("ORBIT"))
                {
                    vd.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();
                }

                // Load crew data
                foreach (ConfigNode crewNode in child.GetNodes("CREW"))
                {
                    CrewData cd = new CrewData();

                    cd.name = ConfigNodeUtil.ParseValue<string>(crewNode, "name", (string)null);
                    cd.addToRoster = ConfigNodeUtil.ParseValue<bool>(crewNode, "addToRoster");

                    vd.crew.Add(cd);
                }

                // Add to the global list
                vessels.Add(vd);
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onGameSceneLoadRequested.Add(new EventData<GameScenes>.OnEvent(OnGameSceneLoad));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
            GameEvents.onGameSceneLoadRequested.Remove(new EventData<GameScenes>.OnEvent(OnGameSceneLoad));
        }

        private void OnVesselRecovered(ProtoVessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselRecovered: " + v);

            // EVA vessel
            if (v.vesselType == VesselType.EVA)
            {
                foreach (ProtoPartSnapshot p in v.protoPartSnapshots)
                {
                    {
                        LoggingUtil.LogVerbose(this, "    p: " + p);
                        foreach (string name in p.protoCrewNames)
                        {
                            // Find this crew member in our data
                            foreach (VesselData vd in vessels)
                            {
                                foreach (CrewData cd in vd.crew)
                                {
                                    if (cd.name == name && cd.addToRoster)
                                    {
                                        // Add them to the roster
                                        ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm.name == cd.name).First();
                                        pcm.type = ProtoCrewMember.KerbalType.Crew;
                                    }
                                }
                            }
                        }
                    }
                }

            }

            // Vessel with crew
            foreach (ProtoCrewMember crewMember in v.GetVesselCrew())
            {
                // Find this crew member in our data
                foreach (VesselData vd in vessels)
                {
                    foreach (CrewData cd in vd.crew)
                    {
                        if (cd.name == crewMember.name && cd.addToRoster)
                        {
                            // Add them to the roster
                            ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm.name == cd.name).First();
                            pcm.type = ProtoCrewMember.KerbalType.Crew;
                        }
                    }

                    // Remove any other crew
                    RemoveCrew(vd);
                }
            }
        }

        private void OnGameSceneLoad(GameScenes gameScene)
        {
            if (deferVesselCreation && (gameScene == GameScenes.FLIGHT || gameScene == GameScenes.TRACKSTATION || gameScene == GameScenes.EDITOR))
            {
                CreateVessels();

                // After the vessels are created, save the game again so we don't lose our changes
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
            }
        }

        protected override void OnAccepted()
        {
            if (!deferVesselCreation)
            {
                CreateVessels();
            }
        }

        protected override void OnCancelled()
        {
            RemoveVessels();
        }

        protected override void OnDeadlineExpired()
        {
            RemoveVessels();
        }

        protected override void OnDeclined()
        {
            RemoveVessels();
        }

        protected override void OnGenerateFailed()
        {
            RemoveVessels();
        }

        protected override void OnOfferExpired()
        {
            RemoveVessels();
        }

        protected override void OnWithdrawn()
        {
            RemoveVessels();
        }

        private void RemoveVessels()
        {
            foreach (VesselData vd in vessels)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v != null && v.id == vd.id);
                if (vessel != null)
                {
                    vessel.state = Vessel.State.DEAD;
                }

                RemoveCrew(vd);
            }

            vessels.Clear();
        }

        private void RemoveCrew(VesselData vd)
        {
            foreach (CrewData cd in vd.crew)
            {
                ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(pcm => pcm.name == cd.name).FirstOrDefault();
                if (!cd.addToRoster && crewMember != null)
                {
                    Vessel otherVessel = FlightGlobals.Vessels.Where(v => v.GetVesselCrew().Contains(crewMember)).FirstOrDefault();
                    if (otherVessel != null)
                    {
                        // If it's an EVA make them disappear...
                        if (otherVessel.isEVA)
                        {
                            FlightGlobals.Vessels.Remove(otherVessel);
                        }
                        else
                        {
                            if (otherVessel.loaded)
                            {
                                foreach (Part p in otherVessel.parts)
                                {
                                    if (p.protoModuleCrew.Contains(crewMember))
                                    {
                                        p.RemoveCrewmember(crewMember);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                foreach (ProtoPartSnapshot pps in otherVessel.protoVessel.protoPartSnapshots)
                                {
                                    if (pps.HasCrew(crewMember.name))
                                    {
                                        pps.RemoveCrew(crewMember);
                                    }
                                }
                            }
                        }
                    }

                    // Remove the kerbal from the roster
                    HighLogic.CurrentGame.CrewRoster.Remove(cd.name);
                }
            }
        }

        private ConfigNode GetNodeForPart(ProtoPartSnapshot p)
        {
            ConfigNode node = new ConfigNode("PART");
            p.Save(node);
            return node;
        }

        public Kerbal GetKerbal(int index)
        {
            int current = index;
            foreach (VesselData vd in vessels)
            {
                if (current < vd.crew.Count)
                {
                    return new Kerbal(HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm.name == vd.crew[current].name).First());
                }
                current -= vd.crew.Count;
            }

            throw new Exception("ContractConfigurator: index " + index +
                " is out of range for number of Kerbals spawned (" + KerbalCount + ").");
        }
        
        public IEnumerable<string> KerbalNames()
        {
            foreach (VesselData vd in vessels)
            {
                foreach (CrewData cd in vd.crew)
                {
                    yield return cd.name;
                }
            }
        }
    }
}