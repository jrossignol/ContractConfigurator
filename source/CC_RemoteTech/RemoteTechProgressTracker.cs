using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using RemoteTech;
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
        public static RemoteTechProgressTracker Instance { get; private set; }

        private class CelestialBodyInfo
        {
            public CelestialBody body;
            public VesselSatellite sat;
            public bool hasCoverage = false;
            public double activeRange = 0.0;
        }
        private Dictionary<CelestialBody, CelestialBodyInfo> celestialBodies = new Dictionary<CelestialBody, CelestialBodyInfo>();

        private bool initialized = false;

        public RemoteTechProgressTracker()
        {
            Instance = this;
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
                        cbi.hasCoverage = false;
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
                // Targetting planet
                else
                {
                    Dictionary<Guid, CelestialBody> planets = RTCore.Instance.Network.Planets;
                    if (planets.ContainsKey(a.Target))
                    {
                        CelestialBodyInfo targetPlanetCBI = celestialBodies[planets[a.Target]];
                        double distance = targetPlanetCBI.sat.DistanceTo(s);
                        if (distance < a.Dish)
                        {
                            targetPlanetCBI.hasCoverage = true;
                        }
                    }

                }
            }

        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            foreach (ConfigNode child in node.GetNodes("CelestialBodyInfo"))
            {
                CelestialBodyInfo cbi = new CelestialBodyInfo();
                cbi.body = ConfigNodeUtil.ParseValue<CelestialBody>(node, "body");
                cbi.hasCoverage = ConfigNodeUtil.ParseValue<bool>(node, "hasCoverage");
                cbi.activeRange = ConfigNodeUtil.ParseValue<double>(node, "activeRange");
                celestialBodies[cbi.body] = cbi;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (CelestialBodyInfo cbi in celestialBodies.Values)
            {
                ConfigNode child = new ConfigNode("CelestialBodyInfo");
                child.AddValue("body", cbi.body.name);
                child.AddValue("hasCoverage", cbi.hasCoverage);
                child.AddValue("activeRange", cbi.activeRange);
                node.AddNode(child);
            }
        }

        /// <summary>
        /// Checks whether there is a satellite targetting the given body.
        /// </summary>
        /// <param name="body">The body to check</param>
        /// <returns>Whether a satellite is targetting the body.</returns>
        public bool HasCoverage(CelestialBody body)
        {
            return celestialBodies.ContainsKey(body) ? celestialBodies[body].hasCoverage : false;
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
    }
}
