using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using FinePrint;
using FinePrint.Utilities;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Class for spawning a waypoint.
    /// </summary>
    public class WaypointGenerator : ContractBehaviour
    {
        private class WaypointData
        {
            public Waypoint waypoint = new Waypoint();
            public string type = null;
            public bool randomAltitude = false;
            public bool waterAllowed = true;
            public bool forceEquatorial = false;
            public bool randomName = false;
            public int nearIndex = -1;
            public double minDistance = 0.0;
            public double maxDistance = 0.0;
            public PQSCity pqsCity = null;
            public Vector3d pqsOffset;
            public string parameter = "";
            public int count = 1;
            public bool underwater = false;

            public WaypointData()
            {
            }

            public WaypointData(string type)
            {
                this.type = type;
            }

            public WaypointData(WaypointData orig, Contract contract)
            {
                type = orig.type;
                waypoint.altitude = orig.waypoint.altitude;
                waypoint.celestialName = orig.waypoint.celestialName;
                waypoint.height = orig.waypoint.height;
                waypoint.id = orig.waypoint.id;
                waypoint.index = orig.waypoint.index;
                waypoint.isClustered = orig.waypoint.isClustered;
                waypoint.isExplored = orig.waypoint.isExplored;
                waypoint.isOnSurface = orig.waypoint.isOnSurface;
                waypoint.landLocked = orig.waypoint.landLocked;
                waypoint.latitude = orig.waypoint.latitude;
                waypoint.longitude = orig.waypoint.longitude;
                waypoint.name = orig.waypoint.name;
                waypoint.visible = orig.waypoint.visible;

                randomAltitude = orig.randomAltitude;
                waterAllowed = orig.waterAllowed;
                forceEquatorial = orig.forceEquatorial;
                nearIndex = orig.nearIndex;
                count = orig.count;
                minDistance = orig.minDistance;
                maxDistance = orig.maxDistance;
                pqsCity = orig.pqsCity;
                pqsOffset = orig.pqsOffset;
                parameter = orig.parameter;
                randomName = orig.randomName;
                underwater = orig.underwater;

                SetContract(contract);
            }

            public void SetContract(Contract contract)
            {
                if (contract != null)
                {
                    waypoint.contractReference = contract;
                    waypoint.seed = contract.MissionSeed;
                }
            }
        }
        private List<WaypointData> waypoints = new List<WaypointData>();
        private bool initialized = false;
        private static System.Random random = new System.Random();

        public WaypointGenerator() { }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public WaypointGenerator(WaypointGenerator orig, ConfiguredContract contract)
            : base()
        {
            foreach (WaypointData old in orig.waypoints)
            {
                // Copy waypoint data
                for (int i = 0; i < old.count; i++)
                {
                    waypoints.Add(new WaypointData(old, contract));
                }
            }
            initialized = orig.initialized;
            orig.initialized = false;
            this.contract = contract;

            Initialize();
        }

        public void Initialize()
        {
            if (!initialized)
            {
                LoggingUtil.LogVerbose(this, "Initializing waypoint generator.");
                foreach (WaypointData wpData in waypoints)
                {
                    CelestialBody body = FlightGlobals.Bodies.Where<CelestialBody>(b => b.name == wpData.waypoint.celestialName).FirstOrDefault();
                    if (body == null)
                    {
                        continue;
                    }

                    // Do type-specific waypoint handling
                    if (wpData.type == "RANDOM_WAYPOINT")
                    {
                        LoggingUtil.LogVerbose(this, "   Generating a random waypoint...");

                        while (true)
                        {
                            // Generate the position
                            WaypointManager.ChooseRandomPosition(out wpData.waypoint.latitude, out wpData.waypoint.longitude,
                                wpData.waypoint.celestialName, wpData.waterAllowed, wpData.forceEquatorial, random);
                            
                            // Force a water waypoint
                            if (wpData.underwater)
                            {
                                Vector3d radialVector = QuaternionD.AngleAxis(wpData.waypoint.longitude, Vector3d.down) *
                                  QuaternionD.AngleAxis(wpData.waypoint.latitude, Vector3d.forward) * Vector3d.right;
                                if (body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius >= 0.0)
                                {
                                    continue;
                                }
                            }
                            break;
                        }
                    }
                    else if (wpData.type == "RANDOM_WAYPOINT_NEAR")
                    {
                        Waypoint nearWaypoint = waypoints[wpData.nearIndex].waypoint;

                        LoggingUtil.LogVerbose(this, "   Generating a random waypoint near waypoint " + nearWaypoint.name + "...");

                        // TODO - this is really bad, we need to implement this method ourselves...
                        do
                        {
                            WaypointManager.ChooseRandomPositionNear(out wpData.waypoint.latitude, out wpData.waypoint.longitude,
                                nearWaypoint.latitude, nearWaypoint.longitude, wpData.waypoint.celestialName,
                                wpData.maxDistance, wpData.waterAllowed, random);

                            // Force a water waypoint
                            if (wpData.underwater)
                            {
                                Vector3d radialVector = QuaternionD.AngleAxis(wpData.waypoint.longitude, Vector3d.down) *
                                  QuaternionD.AngleAxis(wpData.waypoint.latitude, Vector3d.forward) * Vector3d.right;
                                if (body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius >= 0.0)
                                {
                                    continue;
                                }
                            }
                        } while (WaypointUtil.GetDistance(wpData.waypoint.latitude, wpData.waypoint.longitude, nearWaypoint.latitude, nearWaypoint.longitude,
                            body.Radius) < wpData.minDistance);
                    }
                    else if (wpData.type == "PQS_CITY")
                    {
                        LoggingUtil.LogVerbose(this, "   Generating a waypoint based on PQS city " + wpData.pqsCity.name + "...");

                        Vector3d position = wpData.pqsCity.transform.position;

                        // Translate by the PQS offset (inverse transform of coordinate system)
                        Vector3d v = wpData.pqsOffset;
                        Vector3d i = wpData.pqsCity.transform.right;
                        Vector3d j = wpData.pqsCity.transform.forward;
                        Vector3d k = wpData.pqsCity.transform.up;
                        Vector3d offsetPos = new Vector3d(
                            (j.y * k.z - j.z * k.y) * v.x + (i.z * k.y - i.y * k.z) * v.y + (i.y * j.z - i.z * j.y) * v.z,
                            (j.z * k.x - j.x * k.z) * v.x + (i.x * k.z - i.z * k.x) * v.y + (i.z * j.x - i.x * j.z) * v.z,
                            (j.x * k.y - j.y * k.x) * v.x + (i.y * k.x - i.x * k.y) * v.y + (i.x * j.y - i.y * j.x) * v.z
                        );
                        offsetPos *= (i.x * j.y * k.z) + (i.y * j.z * k.x) + (i.z * j.x * k.y) - (i.z * j.y * k.x) - (i.y * j.x * k.z) - (i.x * j.z * k.y);
                        wpData.waypoint.latitude = body.GetLatitude(position + offsetPos);
                        wpData.waypoint.longitude = body.GetLongitude(position + offsetPos);
                    }

                    // Set altitude
                    if (wpData.randomAltitude)
                    {
                        if (wpData.underwater && body.ocean)
                        {
                            Vector3d radialVector = QuaternionD.AngleAxis(wpData.waypoint.longitude, Vector3d.down) *
                              QuaternionD.AngleAxis(wpData.waypoint.latitude, Vector3d.forward) * Vector3d.right;
                            double oceanFloor = body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius;

                            wpData.waypoint.altitude = random.NextDouble() * (oceanFloor);
                        }
                        else if (body.atmosphere)
                        {
                            wpData.waypoint.altitude = random.NextDouble() * (body.atmosphereDepth);
                        }
                        else
                        {
                            wpData.waypoint.altitude = 0.0;
                        }
                    }
                    // Clamp underwater waypoints to sea-floor
                    else if (wpData.underwater)
                    {
                        Vector3d radialVector = QuaternionD.AngleAxis(wpData.waypoint.longitude, Vector3d.down) *
                          QuaternionD.AngleAxis(wpData.waypoint.latitude, Vector3d.forward) * Vector3d.right;
                        double oceanFloor = body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius;

                        wpData.waypoint.altitude = Math.Max(oceanFloor, wpData.waypoint.altitude);
                    }

                    // Set name
                    if (wpData.randomName)
                    {
                        wpData.waypoint.name = StringUtilities.GenerateSiteName(random.Next(), body, !wpData.waterAllowed);
                    }

                    LoggingUtil.LogVerbose(this, "   Generated waypoint " + wpData.waypoint.name + " at " +
                        wpData.waypoint.latitude + ", " + wpData.waypoint.longitude + ".");
                }

                initialized = true;
                LoggingUtil.LogVerbose(this, "Waypoint generator initialized.");
            }
        }

        public void Uninitialize()
        {
            if (initialized)
            {
                initialized = false;
            }
        }

        public static WaypointGenerator Create(ConfigNode configNode, WaypointGeneratorFactory factory)
        {
            WaypointGenerator wpGenerator = new WaypointGenerator();

            bool valid = true;
            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode))
            {
                DataNode dataNode = new DataNode("WAYPOINT_" + index, factory.dataNode, factory);
                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);
                    dataNode["type"] = child.name;

                    double? altitude = null;
                    WaypointData wpData = new WaypointData(child.name);

                    // Use an expression to default - then it'll work for dynamic contracts
                    if (!child.HasValue("targetBody"))
                    {
                        child.AddValue("targetBody", "@/targetBody.Name()");
                    }
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "targetBody", x => wpData.waypoint.celestialName = x, factory, Validation.CheckCelestialBody);

                    valid &= ConfigNodeUtil.ParseValue<string>(child, "name", x => wpData.waypoint.name = x, factory, (string)null);
                    valid &= ConfigNodeUtil.ParseValue<double?>(child, "altitude", x => altitude = x, factory, (double?)null);
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "parameter", x => wpData.parameter = x, factory, "");
                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "hidden", x => wpData.waypoint.visible = !x, factory, false);
                    if (!wpData.waypoint.visible)
                    {
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "icon", x => wpData.waypoint.id = x, factory, "");
                    }
                    else
                    {
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "icon", x => wpData.waypoint.id = x, factory);
                    }

                    // The FinePrint logic is such that it will only look in Squad/Contracts/Icons for icons.
                    // Cheat this by hacking the path in the game database.
                    if (wpData.waypoint.id.Contains("/"))
                    {
                        GameDatabase.TextureInfo texInfo = GameDatabase.Instance.databaseTexture.Where(t => t.name == wpData.waypoint.id).FirstOrDefault();
                        if (texInfo != null)
                        {
                            texInfo.name = "Squad/Contracts/Icons/" + wpData.waypoint.id;
                        }
                    }

                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "underwater", x => wpData.underwater = x, factory, false);

                    // Track the index
                    wpData.waypoint.index = index++;

                    // Get altitude
                    if (altitude == null)
                    {
                        wpData.waypoint.altitude = 0.0;
                        wpData.randomAltitude = true;
                    }
                    else
                    {
                        wpData.waypoint.altitude = altitude.Value;
                    }

                    // Get settings that differ by type
                    if (child.name == "WAYPOINT")
                    {
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "latitude", x => wpData.waypoint.latitude = x, factory);
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "longitude", x => wpData.waypoint.longitude = x, factory);
                    }
                    else if (child.name == "RANDOM_WAYPOINT")
                    {
                        // Get settings for randomization
                        valid &= ConfigNodeUtil.ParseValue<bool>(child, "waterAllowed", x => wpData.waterAllowed = x, factory, true);
                        valid &= ConfigNodeUtil.ParseValue<bool>(child, "forceEquatorial", x => wpData.forceEquatorial = x, factory, false);
                        valid &= ConfigNodeUtil.ParseValue<int>(child, "count", x => wpData.count = x, factory, 1, x => Validation.GE(x, 1));
                    }
                    else if (child.name == "RANDOM_WAYPOINT_NEAR")
                    {
                        // Get settings for randomization
                        valid &= ConfigNodeUtil.ParseValue<bool>(child, "waterAllowed", x => wpData.waterAllowed = x, factory, true);

                        // Get near waypoint details
                        valid &= ConfigNodeUtil.ParseValue<int>(child, "nearIndex", x => wpData.nearIndex = x, factory,
                            x => Validation.GE(x, 0) && Validation.LT(x, wpGenerator.waypoints.Count));
                        valid &= ConfigNodeUtil.ParseValue<int>(child, "count", x => wpData.count = x, factory, 1, x => Validation.GE(x, 1));

                        // Get distances
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "minDistance", x => wpData.minDistance = x, factory, 0.0, x => Validation.GE(x, 0.0));
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "maxDistance", x => wpData.maxDistance = x, factory, x => Validation.GT(x, wpData.minDistance));
                    }
                    else if (child.name == "PQS_CITY")
                    {
                        wpData.randomAltitude = false;
                        string pqsCity = null;
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "pqsCity", x => pqsCity = x, factory);
                        if (pqsCity != null && !string.IsNullOrEmpty(wpData.waypoint.celestialName))
                        {
                            try
                            {
                                CelestialBody body = FlightGlobals.Bodies.Where(b => b.name == wpData.waypoint.celestialName).First();
                                wpData.pqsCity = body.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == pqsCity).First();
                            }
                            catch (Exception e)
                            {
                                LoggingUtil.LogError(typeof(WaypointGenerator), "Couldn't load PQSCity with name '" + pqsCity + "'");
                                LoggingUtil.LogException(e);
                                valid = false;
                            }
                        }
                        valid &= ConfigNodeUtil.ParseValue<Vector3d>(child, "pqsOffset", x => wpData.pqsOffset = x, factory, new Vector3d());
                    }
                    else
                    {
                        LoggingUtil.LogError(factory, "Unrecognized waypoint node: '" + child.name + "'");
                        valid = false;
                    }

                    // Check for unexpected values
                    valid &= ConfigNodeUtil.ValidateUnexpectedValues(child, factory);

                    // Generate a random name
                    if (!child.HasValue("name"))
                    {
                        wpData.randomName = true;
                    }

                    // Add to the list
                    wpGenerator.waypoints.Add(wpData);
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(factory.dataNode);
                }
            }

            return valid ? wpGenerator : null;
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            foreach (WaypointData wpData in waypoints)
            {
                WaypointManager.RemoveWaypoint(wpData.waypoint);
            }

        }

        protected override void OnOffered()
        {
            foreach (WaypointData wpData in waypoints)
            {
                if (wpData.waypoint.visible && (string.IsNullOrEmpty(wpData.parameter) || contract.AllParameters.
                    Where(p => p.ID == wpData.parameter && p.State == ParameterState.Complete).Any()))
                {
                    AddWayPoint(wpData.waypoint);
                }
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (ConfigNode child in configNode.GetNodes("WAYPOINT"))
            {
                // Read all the waypoint data
                WaypointData wpData = new WaypointData();
                wpData.type = child.GetValue("type");
                wpData.parameter = ConfigNodeUtil.ParseValue<string>(child, "parameter", "");
                wpData.waypoint.celestialName = child.GetValue("celestialName");
                wpData.waypoint.name = child.GetValue("name");
                wpData.waypoint.id = child.GetValue("icon");
                wpData.waypoint.latitude = Convert.ToDouble(child.GetValue("latitude"));
                wpData.waypoint.longitude = Convert.ToDouble(child.GetValue("longitude"));
                wpData.waypoint.altitude = Convert.ToDouble(child.GetValue("altitude"));
                wpData.waypoint.index = Convert.ToInt32(child.GetValue("index"));
                wpData.waypoint.visible = !(ConfigNodeUtil.ParseValue<bool?>(child, "hidden", (bool?)false).Value);

                // Set contract data
                wpData.SetContract(contract);

                // Create additional waypoint details
                if (wpData.waypoint.visible && (string.IsNullOrEmpty(wpData.parameter) || contract.AllParameters.
                    Where(p => p.ID == wpData.parameter && p.State == ParameterState.Complete).Any()))
                {
                    AddWayPoint(wpData.waypoint);
                }

                // Add to the global list
                waypoints.Add(wpData);
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (WaypointData wpData in waypoints)
            {
                ConfigNode child = new ConfigNode("WAYPOINT");

                child.AddValue("type", wpData.type);
                child.AddValue("parameter", wpData.parameter);
                child.AddValue("celestialName", wpData.waypoint.celestialName);
                child.AddValue("name", wpData.waypoint.name);
                child.AddValue("icon", wpData.waypoint.id);
                child.AddValue("latitude", wpData.waypoint.latitude);
                child.AddValue("longitude", wpData.waypoint.longitude);
                child.AddValue("altitude", wpData.waypoint.altitude);
                child.AddValue("index", wpData.waypoint.index);
                child.AddValue("hidden", !wpData.waypoint.visible);

                configNode.AddNode(child);
            }
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            if (param.State == ParameterState.Complete)
            {
                foreach (WaypointData wpData in waypoints)
                {
                    if (wpData.waypoint.visible && wpData.parameter == param.ID)
                    {
                        AddWayPoint(wpData.waypoint);
                    }
                }
            }
        }

        private void AddWayPoint(Waypoint waypoint)
        {
            // No contract, no waypoint
            if (waypoint.contractReference == null)
            {
                return;
            }

            // Always surface and navigatable
            waypoint.isOnSurface = true;
            waypoint.isNavigatable = true;

            // Show only active waypoints in flight, but show offered as well in the tracking station
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && contract.ContractState == Contract.State.Active ||
                HighLogic.LoadedScene == GameScenes.TRACKSTATION &&
                (contract.ContractState == Contract.State.Offered || contract.ContractState == Contract.State.Active))
            {
                WaypointManager.AddWaypoint(waypoint);
            }
        }

        public Waypoint GetWaypoint(int index)
        {
            return waypoints[index].waypoint;
        }

        public IEnumerable<Waypoint> Waypoints()
        {
            foreach (WaypointData wpd in waypoints)
            {
                yield return wpd.waypoint;
            }
        }
    }
}
