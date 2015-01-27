using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using FinePrint;
using FinePrint.Utilities;

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
            public int nearIndex = -1;
            public double nearDistance = 0.0;
            public PQSCity pqsCity = null;

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

                randomAltitude = orig.randomAltitude;
                waterAllowed = orig.waterAllowed;
                forceEquatorial = orig.forceEquatorial;
                nearIndex = orig.nearIndex;
                nearDistance = orig.nearDistance;
                pqsCity = orig.pqsCity;
                
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
        
        public WaypointGenerator() {}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public WaypointGenerator(WaypointGenerator orig, Contract contract)
            : base()
        {
            foreach (WaypointData old in orig.waypoints)
            {
                // Copy waypoint data
                waypoints.Add(new WaypointData(old, contract));
            }
        }

        public static WaypointGenerator Create(ConfigNode configNode, CelestialBody defaultBody, WaypointGeneratorFactory factory)
        {
            WaypointGenerator wpGenerator = new WaypointGenerator();

            bool valid = true;
            int index = 0;
            foreach (ConfigNode child in configNode.GetNodes())
            {
                int count = child.HasValue("count") ? Convert.ToInt32(child.GetValue("count")) : 1;
                for (int i = 0; i < count; i++)
                {
                    double? altitude = null;
                    WaypointData wpData = new WaypointData(child.name);

                    valid &= ConfigNodeUtil.ParseValue<string>(child, "targetBody", ref wpData.waypoint.celestialName, factory, defaultBody != null ? defaultBody.name : null, Validation.NotNull);
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "name", ref wpData.waypoint.name, factory, (string)null);
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "icon", ref wpData.waypoint.id, factory);
                    valid &= ConfigNodeUtil.ParseValue<double?>(child, "altitude", ref altitude, factory, (double?)null);

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
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "latitude", ref wpData.waypoint.latitude, factory);
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "longitude", ref wpData.waypoint.longitude, factory);
                    }
                    else if (child.name == "RANDOM_WAYPOINT")
                    {
                        // Get settings for randomization
                        valid &= ConfigNodeUtil.ParseValue<bool>(child, "waterAllowed", ref wpData.waterAllowed, factory, true);
                        valid &= ConfigNodeUtil.ParseValue<bool>(child, "forceEquatorial", ref wpData.forceEquatorial, factory, false);
                    }
                    else if (child.name == "RANDOM_WAYPOINT_NEAR")
                    {
                        // Get settings for randomization
                        valid &= ConfigNodeUtil.ParseValue<bool>(child, "waterAllowed", ref wpData.waterAllowed, factory, true);

                        // Get near waypoint details
                        valid &= ConfigNodeUtil.ParseValue<int>(child, "nearIndex", ref wpData.nearIndex, factory, x => Validation.GE(x, 0));
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "nearDistance", ref wpData.nearDistance, factory, x => Validation.GT(x, 0.0));
                    }
                    else if (child.name == "PQS_CITY")
                    {
                        wpData.randomAltitude = false;
                        string pqsCity = null;
                        valid &= ConfigNodeUtil.ParseValue<string>(child, "pqsCity", ref pqsCity, factory);
                        if (pqsCity != null)
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
                    }
                    else
                    {
                        throw new ArgumentException("Unrecognized waypoint node: '" + child.name + "'");
                    }

                    // Add to the list
                    wpGenerator.waypoints.Add(wpData);
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
            System.Random random = new System.Random(contract.MissionSeed);

            int i = 0;
            foreach (WaypointData wpData in waypoints)
            {
                // Do type-specific waypoint handling
                if (wpData.type == "RANDOM_WAYPOINT")
                {
                    // Generate the position
                    WaypointManager.ChooseRandomPosition(out wpData.waypoint.latitude, out wpData.waypoint.longitude,
                        wpData.waypoint.celestialName, wpData.waterAllowed, wpData.forceEquatorial, random);
                }
                else if (wpData.type == "RANDOM_WAYPOINT_NEAR")
                {
                    Waypoint nearWaypoint = waypoints[wpData.nearIndex].waypoint;
                    WaypointManager.ChooseRandomPositionNear(out wpData.waypoint.latitude, out wpData.waypoint.longitude,
                        nearWaypoint.latitude, nearWaypoint.longitude, wpData.waypoint.celestialName,
                        wpData.nearDistance, wpData.waterAllowed, random);
                }
                else if (wpData.type == "PQS_CITY")
                {
                    CelestialBody body = FlightGlobals.Bodies.Where(b => b.name == wpData.waypoint.celestialName).First();
                    wpData.waypoint.latitude = body.GetLatitude(wpData.pqsCity.transform.position);
                    wpData.waypoint.longitude = body.GetLongitude(wpData.pqsCity.transform.position);
                }

                // Set altitude
                if (wpData.randomAltitude)
                {
                    CelestialBody body = FlightGlobals.Bodies.Where<CelestialBody>(b => b.name == wpData.waypoint.celestialName).First();
                    if (body.atmosphere)
                    {
                        wpData.waypoint.altitude = random.NextDouble() * (body.maxAtmosphereAltitude);
                    }
                    else
                    {
                        wpData.waypoint.altitude = 0.0;
                    }
                }

                // Set name
                if (string.IsNullOrEmpty(wpData.waypoint.name))
                {
                    CelestialBody body = FlightGlobals.Bodies.Where<CelestialBody>(b => b.name == wpData.waypoint.celestialName).First();
                    wpData.waypoint.name = StringUtilities.GenerateSiteName(contract.MissionSeed + i++, body, !wpData.waterAllowed);
                }

                AddWayPoint(wpData.waypoint);
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
                wpData.waypoint.celestialName = child.GetValue("celestialName");
                wpData.waypoint.name = child.GetValue("name");
                wpData.waypoint.id = child.GetValue("icon");
                wpData.waypoint.latitude = Convert.ToDouble(child.GetValue("latitude"));
                wpData.waypoint.longitude = Convert.ToDouble(child.GetValue("longitude"));
                wpData.waypoint.altitude = Convert.ToDouble(child.GetValue("altitude"));
                wpData.waypoint.index = Convert.ToInt32(child.GetValue("index"));

                // Set contract data
                wpData.SetContract(contract);

                // Create additional waypoint details
                AddWayPoint(wpData.waypoint);

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
                child.AddValue("celestialName", wpData.waypoint.celestialName);
                child.AddValue("name", wpData.waypoint.name);
                child.AddValue("icon", wpData.waypoint.id);
                child.AddValue("latitude", wpData.waypoint.latitude);
                child.AddValue("longitude", wpData.waypoint.longitude);
                child.AddValue("altitude", wpData.waypoint.altitude);
                child.AddValue("index", wpData.waypoint.index);

                configNode.AddNode(child);
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
    }
}
