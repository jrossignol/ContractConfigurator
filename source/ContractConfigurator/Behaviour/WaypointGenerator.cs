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
            public List<string> names = new List<string>();
            public string type = null;
            public bool randomAltitude = false;
            public bool waterAllowed = true;
            public bool forceEquatorial = false;
            public int nearIndex = -1;
            public bool chained = false;
            public double minDistance = 0.0;
            public double maxDistance = 0.0;
            public PQSCity pqsCity = null;
            public Vector3d pqsOffset;
            public LaunchSite launchSite = null;
            public List<string> parameter = new List<string>();
            public int count = 1;
            public bool underwater = false;
            public bool isAdded = false;

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
                names = orig.names.ToList();
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
                chained = orig.chained;
                count = orig.count;
                minDistance = orig.minDistance;
                maxDistance = orig.maxDistance;
                pqsCity = orig.pqsCity;
                pqsOffset = orig.pqsOffset;
                launchSite = orig.launchSite;
                parameter = orig.parameter.ToList();
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

        public WaypointGenerator()
        {
            GameEvents.OnMapViewFiltersModified.Add(new EventData<MapViewFiltering.VesselTypeFilter>.OnEvent(OnMapViewFiltersModified));
        }

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
                    WaypointData wpData = new WaypointData(old, contract);
                    waypoints.Add(wpData);

                    // Set the name
                    if (old.names.Any())
                    {
                        wpData.waypoint.name = (old.names.Count() == 1 ? old.names.First() : old.names.ElementAtOrDefault(i));
                    }
                    if (string.IsNullOrEmpty(wpData.waypoint.name) || wpData.waypoint.name.ToLower() == "site")
                    {
                        wpData.waypoint.name = StringUtilities.GenerateSiteName(random.Next(), wpData.waypoint.celestialBody, !wpData.waterAllowed);
                    }

                    // Handle waypoint chaining
                    if (wpData.chained && i != 0)
                    {
                        wpData.nearIndex = waypoints.Count - 2;
                    }
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

                        if (!nearWaypoint.celestialBody.hasSolidSurface)
                            wpData.waterAllowed = true;


                        // Convert input to radians
                        double rlat1 = nearWaypoint.latitude * Math.PI / 180.0;
                        double rlon1 = nearWaypoint.longitude * Math.PI / 180.0;

                        for (int i = 0; i < 10000; i++)
                        {
                            // Sliding window
                            double window = (i < 100 ? 1 : (1.01 * (i-100+1)));
                            double max = wpData.maxDistance * window;
                            double min = wpData.minDistance / window;

                            // Distance between our point and the random waypoint
                            double d = min + random.NextDouble() * (max - min);

                            // Random bearing
                            double brg = random.NextDouble() * 2.0 * Math.PI;

                            // Angle between our point and the random waypoint
                            double a = d / nearWaypoint.celestialBody.Radius;

                            // Calculate the coordinates
                            double rlat2 = Math.Asin(Math.Sin(rlat1) * Math.Cos(a) + Math.Cos(rlat1) * Math.Sin(a) * Math.Cos(brg));
                            double rlon2;

                            // Check for pole
                            if (Math.Abs(Math.Cos(rlat1)) < 0.0001)
                            {
                                rlon2 = rlon1;
                            }
                            else
                            {
                                rlon2 = ((rlon1 - Math.Asin(Math.Sin(brg) * Math.Sin(a) / Math.Cos(rlat2)) + Math.PI) % (2.0 * Math.PI)) - Math.PI;
                            }

                            wpData.waypoint.latitude = rlat2 * 180.0 / Math.PI;
                            wpData.waypoint.longitude = rlon2 * 180.0 / Math.PI;

                            // Calculate the waypoint altitude
                            Vector3d radialVector = QuaternionD.AngleAxis(wpData.waypoint.longitude, Vector3d.down) *
                              QuaternionD.AngleAxis(wpData.waypoint.latitude, Vector3d.forward) * Vector3d.right;
                            double altitude = body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius;

                            // Check water conditions if required
                            if (!nearWaypoint.celestialBody.hasSolidSurface || !nearWaypoint.celestialBody.ocean || (wpData.waterAllowed && !wpData.underwater)|| (wpData.underwater && altitude < 0) || (!wpData.underwater && altitude > 0))
                            {
                                break;
                            }
                        }
                    }
                    else if (wpData.type == "PQS_CITY")
                    {
                        GeneratePQSCityCoordinates(wpData, body);
                    }
                    else if (wpData.type == "LAUNCH_SITE")
                    {
                        GenerateLaunchSiteCoordinates(wpData, body);
                    }

                    // Set altitude
                    SetAltitude(wpData, body);

                    LoggingUtil.LogVerbose(this, "   Generated waypoint " + wpData.waypoint.name + " at " +
                        wpData.waypoint.latitude + ", " + wpData.waypoint.longitude + ".");
                }

                initialized = true;
                LoggingUtil.LogVerbose(this, "Waypoint generator initialized.");
            }
        }

        private void GeneratePQSCityCoordinates(WaypointData wpData, CelestialBody body)
        {
            LoggingUtil.LogVerbose(this, "   pqs city: " + wpData.pqsCity);
            LoggingUtil.LogVerbose(this, "   Generating a waypoint based on PQS city " + wpData.pqsCity.name + "...");

            Vector3d position = wpData.pqsCity.transform.position;
            LoggingUtil.LogVerbose(this, "    pqs city position = " + position);

            // Translate by the PQS offset (inverse transform of coordinate system)
            Vector3d v = wpData.pqsOffset;
            Vector3d i = wpData.pqsCity.transform.right;
            Vector3d j = wpData.pqsCity.transform.forward;
            Vector3d k = wpData.pqsCity.transform.up;
            LoggingUtil.LogVerbose(this, "    i, j, k = " + i + ", " + j + "," + k);
            LoggingUtil.LogVerbose(this, "    pqs transform = " + wpData.pqsCity.transform);
            LoggingUtil.LogVerbose(this, "    parent transform = " + wpData.pqsCity.transform.parent);
            Vector3d offsetPos = new Vector3d(
                (j.y * k.z - j.z * k.y) * v.x + (i.z * k.y - i.y * k.z) * v.y + (i.y * j.z - i.z * j.y) * v.z,
                (j.z * k.x - j.x * k.z) * v.x + (i.x * k.z - i.z * k.x) * v.y + (i.z * j.x - i.x * j.z) * v.z,
                (j.x * k.y - j.y * k.x) * v.x + (i.y * k.x - i.x * k.y) * v.y + (i.x * j.y - i.y * j.x) * v.z
            );
            offsetPos *= (i.x * j.y * k.z) + (i.y * j.z * k.x) + (i.z * j.x * k.y) - (i.z * j.y * k.x) - (i.y * j.x * k.z) - (i.x * j.z * k.y);
            wpData.waypoint.latitude = body.GetLatitude(position + offsetPos);
            wpData.waypoint.longitude = body.GetLongitude(position + offsetPos);
            LoggingUtil.LogVerbose(this, "    resulting lat, lon = (" + wpData.waypoint.latitude + ", " + wpData.waypoint.longitude + ")");
        }

        private void GenerateLaunchSiteCoordinates(WaypointData wpData, CelestialBody body)
        {
            LoggingUtil.LogVerbose(this, "   launch site: " + wpData.launchSite.name);
            LoggingUtil.LogVerbose(this, "   Generating a waypoint based on launch site " + wpData.launchSite.name + "...");

            LaunchSite.SpawnPoint spawnPoint = wpData.launchSite.GetSpawnPoint(wpData.launchSite.name);
            wpData.waypoint.latitude = spawnPoint.latitude;
            wpData.waypoint.longitude = spawnPoint.longitude;

            LoggingUtil.LogVerbose(this, "    resulting lat, lon = (" + wpData.waypoint.latitude + ", " + wpData.waypoint.longitude + ")");
        }

        private void SetAltitude(WaypointData wpData, CelestialBody body)
        {
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

            // Waypoint Manager integration
            EventData<string> onWaypointIconAdded = GameEvents.FindEvent<EventData<string>>("OnWaypointIconAdded");

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
                        child.AddValue("targetBody", "@/targetBody");
                    }
                    valid &= ConfigNodeUtil.ParseValue<CelestialBody>(child, "targetBody", x => wpData.waypoint.celestialName = x != null ? x.name : "", factory);

                    valid &= ConfigNodeUtil.ParseValue<List<string>>(child, "name", x => wpData.names = x, factory, new List<string>());
                    valid &= ConfigNodeUtil.ParseValue<double?>(child, "altitude", x => altitude = x, factory, (double?)null);
                    valid &= ConfigNodeUtil.ParseValue<List<string>>(child, "parameter", x => wpData.parameter = x, factory, new List<string>());
                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "hidden", x => wpData.waypoint.visible = !x, factory, false);

                    Action<string> assignWaypoint = (x) =>
                    {
                        wpData.waypoint.id = x;
                        if (onWaypointIconAdded != null)
                        {
                            onWaypointIconAdded.Fire(x);
                        }
                    };
                    if (!wpData.waypoint.visible)
                    {
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "icon", assignWaypoint, factory, "");
                    }
                    else
                    {
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "icon", assignWaypoint, factory);
                    }

                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "underwater", x => wpData.underwater = x, factory, false);
                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "clustered", x => wpData.waypoint.isClustered = x, factory, false);

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
                        valid &= ConfigNodeUtil.ParseValue<bool>(child, "chained", x => wpData.chained = x, factory, false);
                        valid &= ConfigNodeUtil.ParseValue<int>(child, "count", x => wpData.count = x, factory, 1, x => Validation.GE(x, 1));

                        // Get distances
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "minDistance", x => wpData.minDistance = x, factory, 0.0, x => Validation.GE(x, 0.0));
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "maxDistance", x => wpData.maxDistance = x, factory, x => Validation.GT(x, 0.0));
                    }
                    else if (child.name == "PQS_CITY")
                    {
                        wpData.randomAltitude = false;
                        string dummy = null;
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "pqsCity", x => dummy = x, factory, x =>
                        {
                            bool v = true;
                            if (!string.IsNullOrEmpty(wpData.waypoint.celestialName))
                            {
                                try
                                {
                                    CelestialBody body = FlightGlobals.Bodies.Where(b => b.name == wpData.waypoint.celestialName).First();
                                    wpData.pqsCity = body.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == x).First();
                                }
                                catch (Exception e)
                                {
                                    LoggingUtil.LogError(typeof(WaypointGenerator), "Couldn't load PQSCity with name '" + x + "'");
                                    LoggingUtil.LogException(e);
                                    v = false;
                                }
                            }
                            else
                            {
                                // Force this to get re-run when the targetBody is loaded
                                throw new DataNode.ValueNotInitialized("/targetBody");
                            }
                            return v;
                        });
                        valid &= ConfigNodeUtil.ParseValue<Vector3d>(child, "pqsOffset", x => wpData.pqsOffset = x, factory, new Vector3d());
                    }
                    else if (child.name == "LAUNCH_SITE")
                    {
                        wpData.randomAltitude = false;
                        string dummy = null;
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "launchSite", x => dummy = x, factory, x =>
                        {
                            bool v = true;
                            if (!string.IsNullOrEmpty(wpData.waypoint.celestialName))
                            {
                                try
                                {
                                    wpData.launchSite = ConfigNodeUtil.ParseLaunchSiteValue(x);
                                }
                                catch (Exception e)
                                {
                                    LoggingUtil.LogError(typeof(WaypointGenerator), "Couldn't load Launch Site with name '" + x + "'");
                                    LoggingUtil.LogException(e);
                                    v = false;
                                }
                            }
                            else
                            {
                                // Force this to get re-run when the targetBody is loaded
                                throw new DataNode.ValueNotInitialized("/targetBody");
                            }
                            return v;
                        });
                        valid &= ConfigNodeUtil.ParseValue<Vector3d>(child, "pqsOffset", x => wpData.pqsOffset = x, factory, new Vector3d());
                    }
                    else
                    {
                        LoggingUtil.LogError(factory, "Unrecognized waypoint node: '" + child.name + "'");
                        valid = false;
                    }

                    // Check for unexpected values
                    valid &= ConfigNodeUtil.ValidateUnexpectedValues(child, factory);

                    // Copy waypoint data
                    WaypointData old = wpData;
                    wpData = new WaypointData(old, null);
                    wpGenerator.waypoints.Add(wpData);
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(factory.dataNode);
                }
            }

            return valid ? wpGenerator : null;
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));

            foreach (WaypointData wpData in waypoints)
            {
                WaypointManager.RemoveWaypoint(wpData.waypoint);
            }
        }

        protected void OnMapViewFiltersModified(MapViewFiltering.VesselTypeFilter filter)
        {
            if (filter == MapViewFiltering.VesselTypeFilter.None)
            {
                // Reset state of renderers
                foreach (WaypointData wpData in waypoints)
                {
                    WaypointManager.RemoveWaypoint(wpData.waypoint);
                    wpData.isAdded = false;
                    AddWayPoint(wpData);
                }
            }
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            LoggingUtil.LogVerbose(this, "OnParameterStateChange");

            // Just call OnOffered to add any missing waypoints
            OnOffered();
        }

        protected override void OnOffered()
        {
            foreach (WaypointData wpData in waypoints)
            {
                string paramID = wpData.parameter.FirstOrDefault();
                if (wpData.waypoint.visible && (!wpData.parameter.Any() || contract.AllParameters.
                    Where(p => p.ID == paramID && p.State == ParameterState.Complete).Any()))
                {
                    AddWayPoint(wpData);
                }
            }
        }

        protected void OnFlightReady()
        {
            LoggingUtil.LogVerbose(this, "OnFlightReady");

            // Handle late adjustments to PQS City based waypoint positions (workaround for Kopernicus bug)
            foreach (WaypointData wpData in waypoints)
            {
                if (wpData.pqsCity != null)
                {
                    LoggingUtil.LogDebug(this, "Adjusting PQS City offset coordinates for waypoint " + wpData.waypoint.name);

                    CelestialBody body = FlightGlobals.Bodies.Where(b => b.name == wpData.waypoint.celestialName).First();
                    GeneratePQSCityCoordinates(wpData, body);
                    SetAltitude(wpData, body);
                    wpData.pqsCity = null;
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
                wpData.parameter = ConfigNodeUtil.ParseValue<List<string>>(child, "parameter", new List<string>());
                wpData.names = ConfigNodeUtil.ParseValue<List<string>>(child, "names", new List<string>());
                wpData.waypoint.celestialName = child.GetValue("celestialName");
                wpData.waypoint.name = child.GetValue("name");
                wpData.waypoint.id = child.GetValue("icon");
                wpData.waypoint.latitude = Convert.ToDouble(child.GetValue("latitude"));
                wpData.waypoint.longitude = Convert.ToDouble(child.GetValue("longitude"));
                wpData.waypoint.altitude = Convert.ToDouble(child.GetValue("altitude"));
                wpData.waypoint.index = Convert.ToInt32(child.GetValue("index"));
                wpData.waypoint.visible = !(ConfigNodeUtil.ParseValue<bool?>(child, "hidden", (bool?)false).Value);
                wpData.waypoint.isClustered = ConfigNodeUtil.ParseValue<bool?>(child, "clustered", (bool?)false).Value;

                string pqsCityName = ConfigNodeUtil.ParseValue<string>(child, "pqsCity", null);
                if (pqsCityName != null)
                {
                    CelestialBody body = FlightGlobals.Bodies.Where(b => b.name == wpData.waypoint.celestialName).First();
                    wpData.pqsCity = body.GetComponentsInChildren<PQSCity>(true).Where(pqs => pqs.name == pqsCityName).First();
                    wpData.pqsOffset = ConfigNodeUtil.ParseValue<Vector3d>(child, "pqsOffset");
                }

                wpData.randomAltitude = ConfigNodeUtil.ParseValue<bool?>(child, "randomAltitude", (bool?)false).Value;
                wpData.underwater = ConfigNodeUtil.ParseValue<bool?>(child, "underwater", (bool?)false).Value;

                // Set contract data
                wpData.SetContract(contract);

                // Create additional waypoint details
                string paramID = wpData.parameter.FirstOrDefault();
                if (wpData.waypoint.visible && (!wpData.parameter.Any() || contract.AllParameters.
                    Where(p => p.ID == paramID && p.State == ParameterState.Complete).Any()))
                {
                    AddWayPoint(wpData);
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
                foreach (string p in wpData.parameter)
                {
                    child.AddValue("parameter", p);
                }
                foreach (string n in wpData.names)
                {
                    child.AddValue("names", n);
                }
                child.AddValue("celestialName", wpData.waypoint.celestialName);
                child.AddValue("name", wpData.waypoint.name);
                child.AddValue("icon", wpData.waypoint.id);
                child.AddValue("latitude", wpData.waypoint.latitude);
                child.AddValue("longitude", wpData.waypoint.longitude);
                child.AddValue("altitude", wpData.waypoint.altitude);
                child.AddValue("index", wpData.waypoint.index);
                child.AddValue("hidden", !wpData.waypoint.visible);
                child.AddValue("clustered", wpData.waypoint.isClustered);
                if (wpData.pqsCity != null)
                {
                    child.AddValue("pqsCity", wpData.pqsCity.name);
                    child.AddValue("pqsOffset", wpData.pqsOffset.x + "," + wpData.pqsOffset.y + "," + wpData.pqsOffset.z);
                }
                child.AddValue("randomAltitude", wpData.randomAltitude);
                child.AddValue("underwater", wpData.underwater);

                configNode.AddNode(child);
            }
        }

        private void AddWayPoint(WaypointData wpData)
        {
            if (wpData.isAdded)
            {
                return;
            }

            Waypoint waypoint = wpData.waypoint;

            // No contract, no waypoint
            if (waypoint.contractReference == null)
            {
                return;
            }

            // Always surface and navigatable
            waypoint.isOnSurface = true;
            waypoint.isNavigatable = true;

            // Show only active waypoints in flight, but show offered as well in the tracking station
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                ContractConfiguratorParameters parms = HighLogic.CurrentGame.Parameters.CustomParams<ContractConfiguratorParameters>();

                if (contract.ContractState == Contract.State.Active && (parms.DisplayActiveWaypoints || HighLogic.LoadedScene != GameScenes.TRACKSTATION) ||
                    contract.ContractState == Contract.State.Offered && parms.DisplayOfferedWaypoints && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    WaypointManager.AddWaypoint(waypoint);
                    wpData.isAdded = true;
                }
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
