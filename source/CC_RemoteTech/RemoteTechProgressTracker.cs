using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using RemoteTech;
using RemoteTech.API;
using RemoteTech.SimpleTypes;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// Class for tracking RemoteTech progress for Contract Configurator requirements.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    public class RemoteTechProgressTracker : ScenarioModule
    {
        private static double DIST_FACTOR = 1.0 / Math.Sqrt(3.0);
        private class FakeSatellite : ISatellite
        {
            private class FakeAntenna : IAntenna
            {
                public String Name { get { return "Contract Configurator fake antenna"; } }
                public Guid Guid { get { return new Guid("a5afe68c-9fff-4e12-8f96-b63933e70c99"); } }
                public bool Activated { get { return true; } set { } }
                public bool Powered { get { return true; } }
                public bool Connected { get { return true; } }
                public bool CanTarget { get { return false; } }
                public bool CanRelaySignal { get { return true; } }
                public Guid Target { get { return Guid.Empty; } set { } }
                public float Dish { get { return -1.0f; } }
                public double CosAngle { get { return 1.0f; } }
                public float Omni { get { return float.MaxValue; } }
                public float Consumption { get { return 0.0f; } }

                public void OnConnectionRefresh() { }

                public int CompareTo(IAntenna antenna)
                {
                    return Consumption.CompareTo(antenna.Consumption);
                }
            }

            public bool Visible { get { return false; } }
            public String Name
            {
                get { return "Contract Configurator fake satellite"; }
                set { }
            }
            public Guid Guid { get { return NetworkManager.ActiveVesselGuid; } }
            public Vector3d Position { get; set; }
            public CelestialBody Body { get; set; }
            public Color MarkColor { get { return new Color(0.996078f, 0, 0, 1); } }

            public bool Powered { get { return true; } }
            public bool IsCommandStation { get { return true; } }
            public bool HasLocalControl { get { return true; } }

            /// <summary>
            /// Indicates whether the ISatellite corresponds to a vessel
            /// </summary>
            /// <value><c>true</c> if satellite is vessel or asteroid; otherwise (e.g. a ground station), <c>false</c>.</value>
            public bool isVessel { get { return false; } }

            /// <summary>
            /// The vessel hosting the ISatellite, if one exists.
            /// </summary>
            /// <value>The vessel corresponding to this ISatellite. Returns null if !isVessel.</value>
            public Vessel parentVessel { get { return null; } }

            public IEnumerable<IAntenna> Antennas { get { yield return antenna; } }
            private FakeAntenna antenna = new FakeAntenna();

            public void OnConnectionRefresh(List<NetworkRoute<ISatellite>> routes) { }

            public IEnumerable<NetworkLink<ISatellite>> FindNeighbors(ISatellite s)
            {
                // Special case for finding our own neighbours
                if (s == this)
                {
                    return FindNeighbors();
                }
                // Pass through to the regular function
                else
                {
                    return RTCore.Instance.Network.FindNeighbors(s);
                }
            }

            private IEnumerable<NetworkLink<ISatellite>> FindNeighbors()
            {
                foreach (ISatellite sat in RTCore.Instance.Satellites)
                {
                    NetworkLink<ISatellite> link = NetworkManager.GetLink(this, sat);
                    if (link != null)
                    {
                        yield return link;
                    }
                }
            }

            /// <summary>
            /// Our own version of the distance function, less accurate, but far lower execution cost.
            /// </summary>
            public static double DistanceTo(ISatellite a, ISatellite b)
            {
                return (Math.Abs(a.Position.x - b.Position.x) +
                    Math.Abs(a.Position.y - b.Position.y) +
                    Math.Abs(a.Position.z - b.Position.z)) * DIST_FACTOR;
            }

            /// <summary>
            /// Our own version of the distance function, less accurate, but far lower execution cost.
            /// </summary>
            public static double DistanceTo(ISatellite a, NetworkLink<ISatellite> b)
            {
                return (Math.Abs(a.Position.x - b.Target.Position.x) +
                    Math.Abs(a.Position.y - b.Target.Position.y) +
                    Math.Abs(a.Position.z - b.Target.Position.z)) * DIST_FACTOR;
            }

        }

        public static RemoteTechProgressTracker Instance { get; private set; }

        private FakeSatellite fakeSatellite;

        private int tick = 0;
        private int nextCheck = 0;

        private int UPDATE_INTERVAL = 20;
        private int POINT_COUNT = 20;

        // Prime numbers so that we don't check the points in a straightforward order
        private double LAT_OFFSET = 11.0;
        private double LON_OFFSET = 17.0;

        // Priority system
        private List<CelestialBody> priorityList = new List<CelestialBody>();
        private int nextPriorityCheck = 0;

        private class CelestialBodyInfo
        {
            public CelestialBody body;
            public VesselSatellite sat;
            public UInt32 coverage = 0;
            public double activeRange = 0.0;
        }
        private Dictionary<CelestialBody, CelestialBodyInfo> celestialBodies = new Dictionary<CelestialBody, CelestialBodyInfo>();

        private bool initialized = false;

        public RemoteTechProgressTracker()
        {
            Instance = this;
            fakeSatellite = new FakeSatellite();
        }

        private void Initialize()
        {
            if (!initialized)
            {
                // Add celestial bodies to listing
                foreach (CelestialBody cb in FlightGlobals.Bodies)
                {
                    if (!celestialBodies.ContainsKey(cb))
                    {
                        CelestialBodyInfo cbi = new CelestialBodyInfo();
                        cbi.body = cb;
                        cbi.coverage = 0;
                        celestialBodies[cb] = cbi;
                    }

                    celestialBodies[cb].sat = RTCore.Instance.Satellites[cb.Guid()];
                }
                
                initialized = true;
            }
        }

        public void Start()
        {
            RemoteTechAssistant.OnRemoteTechUpdate.Add(new EventData<VesselSatellite>.OnEvent(OnRemoteTechUpdate));
        }

        public void OnDestroy()
        {
            RemoteTechAssistant.OnRemoteTechUpdate.Remove(new EventData<VesselSatellite>.OnEvent(OnRemoteTechUpdate));
        }

        void FixedUpdate()
        {
            // RemoteTech not loaded, don't do anything
            if (RTCore.Instance == null)
            {
                return;
            }

            Initialize();

            // Check if time to update
            bool priorityUpdate = false;
            if (tick++ % UPDATE_INTERVAL != 0)
            {
                if (priorityList.Count > 0 && tick % UPDATE_INTERVAL == UPDATE_INTERVAL / 2)
                {
                    priorityUpdate = true;
                }
                else
                {
                    return;
                }
            }

            // Get the celestial body and index we'll be checking
            CelestialBody body = null;
            int i = 0;
            if (priorityUpdate)
            {
                body = priorityList[nextPriorityCheck++ % priorityList.Count];
                i = (nextPriorityCheck / priorityList.Count) % POINT_COUNT;
            }
            else
            {
                body = FlightGlobals.Bodies[nextCheck++ % FlightGlobals.Bodies.Count];
                i = (nextCheck / FlightGlobals.Bodies.Count) % POINT_COUNT;
            }
            LoggingUtil.LogVerbose(this, "OnFixedUpdate check (" + priorityUpdate + "): " + body.name + ", point = " + i);

            // Set the position
            fakeSatellite.Position = body.GetWorldSurfacePosition(Math.Sin((i * LAT_OFFSET) / POINT_COUNT * 2.0 * Math.PI) * 45.0, (i * LON_OFFSET) / POINT_COUNT * 360.0, 10000.0);

            // Attempt to find a path
            Func<ISatellite, IEnumerable<NetworkLink<ISatellite>>> neighbors = fakeSatellite.FindNeighbors;
            Func<ISatellite, NetworkLink<ISatellite>, double> cost = FakeSatellite.DistanceTo;
            Func<ISatellite, ISatellite, double> heuristic = FakeSatellite.DistanceTo;
            var path = NetworkPathfinder.Solve(fakeSatellite, RTSettings.Instance.GroundStations[0], neighbors, cost, heuristic);

            // Get the masks for our value
            UInt32 mask = (UInt32)1 << i;

            // Get the coverage info
            CelestialBodyInfo cbi = celestialBodies[body];

            // Update our value
            cbi.coverage = path.Exists ? (cbi.coverage | mask) : (cbi.coverage & ~mask);
        }

        private void OnRemoteTechUpdate(VesselSatellite s)
        {
            Initialize();

            // Don't care about unpowered satellites or satellites that don't call home
            if (!s.Powered || !API.HasConnectionToKSC(s.Guid))
            {
                return;
            }

            // Get info about the body being orbited
            CelestialBodyInfo orbitedCBI = celestialBodies[s.parentVessel.mainBody];

            // Find active
            foreach (IAntenna a in s.Antennas.Where(a => a.Activated))
            {
                // Targetting active vessel
                if (a.Omni > 0.0 || a.Target == NetworkManager.ActiveVesselGuid)
                {
                    orbitedCBI.activeRange = Math.Max(orbitedCBI.activeRange, Math.Max(a.Omni, a.Dish));
                }
            }

        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                base.OnLoad(node);
                nextCheck = ConfigNodeUtil.ParseValue<int>(node, "nextCheck", 0);
                foreach (ConfigNode child in node.GetNodes("CelestialBodyInfo"))
                {
                    CelestialBodyInfo cbi = new CelestialBodyInfo();
                    try
                    {
                        cbi.body = ConfigNodeUtil.ParseValue<CelestialBody>(child, "body");
                    }
                    catch (Exception e)
                    {
                        LoggingUtil.LogWarning(this, "Error loading celestial body, skipping.  Error was:");
                        LoggingUtil.LogException(e);
                        continue;
                    }
                    cbi.coverage = ConfigNodeUtil.ParseValue<UInt32>(child, "coverage", 0);
                    cbi.activeRange = ConfigNodeUtil.ParseValue<double>(child, "activeRange");
                    celestialBodies[cbi.body] = cbi;
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error loading RemoteTechProgressTracker from persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_LOAD, e, "RemoteTechProgressTracker");
            }
        }

        public override void OnSave(ConfigNode node)
        {
            try
            {
                base.OnSave(node);
                node.AddValue("nextCheck", nextCheck);
                foreach (CelestialBodyInfo cbi in celestialBodies.Values)
                {
                    ConfigNode child = new ConfigNode("CelestialBodyInfo");
                    child.AddValue("body", cbi.body.name);
                    child.AddValue("coverage", cbi.coverage);
                    child.AddValue("activeRange", cbi.activeRange);
                    node.AddNode(child);
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error saving RemoteTechProgressTracker to persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_SAVE, e, "RemoteTechProgressTracker");
            }
        }

        /// <summary>
        /// Gets the coverage of the given body.
        /// </summary>
        /// <param name="body">The body to check coverage for.</param>
        /// <returns>The coverage ration (between 0.0 and 1.0)</returns>
        public static double GetCoverage(CelestialBody body)
        {
            // Calculate the coverage
            if (Instance != null && Instance.celestialBodies.ContainsKey(body))
            {
                UInt32 cov = Instance.celestialBodies[body].coverage;
                UInt32 count = 0;
                while (cov > 0)
                {
                    count += cov & 1;
                    cov = cov >> 1;
                }
                return (double)count / Instance.POINT_COUNT;
            }

            return 0.0;
        }

        /// <summary>
        /// Gets the maximum active vessel range of a satellite orbiting the given body.
        /// </summary>
        /// <param name="body">The body to check</param>
        /// <returns>The range</returns>
        public double ActiveRange(CelestialBody body)
        {
            return celestialBodies.ContainsKey(body) ? celestialBodies[body].activeRange : 0.0;
        }

        /// <summary>
        /// Add the given body to the priority list, which will increase check frequency.  Used if
        /// there is an active contract for the given body (otherwise the checks are really
        /// background checks).
        /// </summary>
        /// <param name="body">The body to check more frequently</param>
        public void AddToPriorityList(CelestialBody body)
        {
            priorityList.AddUnique(body);
        }

        /// <summary>
        /// Remove the given body from the priority list.
        /// </summary>
        /// <param name="body">The body to remove</param>
        public void RemoveFromPriorityList(CelestialBody body)
        {
            priorityList.Remove(body);
        }
    }
}
