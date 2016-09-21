using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using FinePrint;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Class for spawning an orbit waypoint.
    /// </summary>
    public class OrbitGenerator : ContractBehaviour
    {
        private class OrbitData
        {
            public Orbit orbit = new Orbit();
            public ContractOrbitRenderer orbitRenderer = null;
            public Contract contract;
            public string type = null;
            public OrbitType orbitType = OrbitType.RANDOM;
            public int count = 1;
            public CelestialBody targetBody;
            public double altitudeFactor;
            public double inclinationFactor;
            public double eccentricity;
            public double deviationWindow;

            public OrbitData()
            {
            }

            public OrbitData(string type)
            {
                this.type = type;
            }

            public OrbitData(OrbitData orig, Contract contract)
            {
                type = orig.type;
                orbitType = orig.orbitType;
                count = orig.count;
                targetBody = orig.targetBody;
                altitudeFactor = orig.altitudeFactor;
                inclinationFactor = orig.inclinationFactor;
                eccentricity = orig.eccentricity;
                deviationWindow = orig.deviationWindow;
                this.contract = contract;

                // Lazy copy of orbit - only really used to store the orbital parameters, so not
                // a huge deal.
                orbit = orig.orbit;
            }

            public void SetupRenderer()
            {
                if (orbitRenderer != null)
                {
                    return;
                }

                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    ContractConfiguratorParameters parms = HighLogic.CurrentGame.Parameters.CustomParams<ContractConfiguratorParameters>();

                    if (contract.ContractState == Contract.State.Active && (parms.DisplayActiveOrbits || HighLogic.LoadedScene != GameScenes.TRACKSTATION) ||
                        contract.ContractState == Contract.State.Offered && parms.DisplayOfferedOrbits && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                    {
                        orbitRenderer = ContractOrbitRenderer.Setup(contract, orbit);
                    }
                }
            }

            public void CleanupRenderer()
            {
                if (orbitRenderer != null)
                {
                    orbitRenderer.Cleanup();
                    orbitRenderer = null;
                }
            }
        }
        private List<OrbitData> orbits = new List<OrbitData>();

        public OrbitGenerator()
        {
            GameEvents.OnMapViewFiltersModified.Add(new EventData<MapViewFiltering.VesselTypeFilter>.OnEvent(OnMapViewFiltersModified));
        }

        public OrbitGenerator(OrbitGenerator orig, Contract contract)
            : base()
        {
            foreach (OrbitData old in orig.orbits)
            {
                for (int i = 0; i < old.count; i++ )
                {
                    // Copy orbit data
                    orbits.Add(new OrbitData(old, contract));
                }
            }

            System.Random random = new System.Random(contract.MissionSeed);

            // Find/add the AlwaysTrue parameter
            AlwaysTrue alwaysTrue = AlwaysTrue.FetchOrAdd(contract);

            int index = 0;
            foreach (OrbitData obData in orbits)
            {
                // Do type specific handling
                if (obData.type == "RANDOM_ORBIT")
                {
                    if (!ValidateOrbitType(obData, null))
                    {

                    }

                    obData.orbit = OrbitUtilities.GenerateOrbit(contract.MissionSeed + index++, obData.targetBody, obData.orbitType,
                        obData.altitudeFactor, obData.inclinationFactor, obData.eccentricity);
                }
                else
                {
                    obData.orbit.referenceBody = obData.targetBody;
                }

                obData.SetupRenderer();
            }
        }

        public static OrbitGenerator Create(ConfigNode configNode, OrbitGeneratorFactory factory)
        {
            OrbitGenerator obGenerator = new OrbitGenerator();

            bool valid = true;
            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode))
            {
                DataNode dataNode = new DataNode("ORBIT_" + index++, factory.dataNode, factory);
                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);

                    OrbitData obData = new OrbitData(child.name);

                    // Get settings that differ by type
                    if (child.name == "FIXED_ORBIT")
                    {
                        valid &= ConfigNodeUtil.ParseValue<Orbit>(child, "ORBIT", x => obData.orbit = x, factory);
                    }
                    else if (child.name == "RANDOM_ORBIT")
                    {
                        valid &= ConfigNodeUtil.ParseValue<OrbitType>(child, "type", x => obData.orbitType = x, factory);
                        valid &= ConfigNodeUtil.ParseValue<int>(child, "count", x => obData.count = x, factory, 1, x => Validation.GE(x, 1));
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "altitudeFactor", x => obData.altitudeFactor = x, factory, 0.8, x => Validation.Between(x, 0.0, 1.0));
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "inclinationFactor", x => obData.inclinationFactor = x, factory, 0.8, x => Validation.Between(x, 0.0, 1.0));
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "eccentricity", x => obData.eccentricity = x, factory, 0.0, x => Validation.GE(x, 0.0));
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "deviationWindow", x => obData.deviationWindow = x, factory, 10.0, x => Validation.GE(x, 0.0));
                    }
                    else
                    {
                        throw new ArgumentException("Unrecognized orbit node: '" + child.name + "'");
                    }

                    // Use an expression to default - then it'll work for dynamic contracts
                    if (!child.HasValue("targetBody"))
                    {
                        child.AddValue("targetBody", "@/targetBody");
                    }
                    valid &= ConfigNodeUtil.ParseValue<CelestialBody>(child, "targetBody", x => obData.targetBody = x, factory);

                    // Check for unexpected values
                    valid &= ConfigNodeUtil.ValidateUnexpectedValues(child, factory);

                    // Add to the list
                    obGenerator.orbits.Add(obData);

                    if (dataNode.IsInitialized("targetBody") && dataNode.IsInitialized("type"))
                    {
                        valid &= obGenerator.ValidateOrbitType(obData, factory);
                    }
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(factory.dataNode);
                }
            }

            return valid ? obGenerator : null;
        }

        private bool ValidateOrbitType(OrbitData obData, OrbitGeneratorFactory factory)
        {
            if (obData.orbitType == OrbitType.KOLNIYA && !CelestialUtilities.CanBodyBeKolniya(obData.targetBody))
            {
                string error = string.Format("Cannot use a Kolniya orbit with {0}.", obData.targetBody.theName);
                if (factory != null)
                {
                    LoggingUtil.LogError(factory, factory.ErrorPrefix() + ": " + error);
                    return false;
                }
                else
                {
                    throw new ArgumentException(error);
                }
            }
            else if (obData.orbitType == OrbitType.TUNDRA && !CelestialUtilities.CanBodyBeTundra(obData.targetBody))
            {
                string error = string.Format("Cannot use a tundra orbit with {0}.", obData.targetBody.theName);
                if (factory != null)
                {
                    LoggingUtil.LogError(factory, factory.ErrorPrefix() + ": " + error);
                    return false;
                }
                else
                {
                    throw new ArgumentException(error);
                }
            }
            return true;
        }

        protected override void OnAccepted()
        {
            foreach (OrbitData obData in orbits)
            {
                obData.SetupRenderer();
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();

        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            foreach (OrbitData obData in orbits)
            {
                obData.CleanupRenderer();
            }
        }

        protected void OnMapViewFiltersModified(MapViewFiltering.VesselTypeFilter filter)
        {
            if (filter == MapViewFiltering.VesselTypeFilter.None)
            {
                // Reset state of renderers
                foreach (OrbitData obData in orbits)
                {
                    ContractConfiguratorParameters parms = HighLogic.CurrentGame.Parameters.CustomParams<ContractConfiguratorParameters>();

                    if (contract.ContractState == Contract.State.Active && parms.DisplayActiveOrbits ||
                        contract.ContractState == Contract.State.Offered && parms.DisplayOfferedOrbits)
                    {
                        obData.SetupRenderer();
                    }
                    else
                    {
                        obData.CleanupRenderer();
                    }
                }
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (ConfigNode child in configNode.GetNodes("ORBIT_DETAIL"))
            {
                // Read all the orbit data
                OrbitData obData = new OrbitData();
                obData.type = child.GetValue("type");

                obData.contract = contract;
                obData.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();
                obData.SetupRenderer();

                // Add to the global list
                orbits.Add(obData);
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (OrbitData obData in orbits)
            {
                ConfigNode child = new ConfigNode("ORBIT_DETAIL");

                child.AddValue("type", obData.type);

                ConfigNode orbitNode = new ConfigNode("ORBIT");
                new OrbitSnapshot(obData.orbit).Save(orbitNode);
                child.AddNode(orbitNode);

                configNode.AddNode(child);
            }
        }

        public Orbit GetOrbit(int index)
        {
            OrbitData obData = orbits.ElementAtOrDefault(index);
            if (obData != null)
            {
                return obData.orbit;
            }

            return null;
        }
    }
}
