using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using FinePrint;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;

namespace ContractConfigurator.Behaviour
{
    /*
     * Class for spawning an orbit waypoint.
     */
    public class OrbitGenerator : ContractBehaviour
    {
        [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
        public class OrbitRenderer : MonoBehaviour
        {
            void Start()
            {
                DontDestroyOnLoad(this);
            }

            public void OnPreCull()
            {
                if (HighLogic.LoadedSceneIsFlight && MapView.MapIsEnabled || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    foreach (OrbitGenerator og in allOrbitGenerators)
                    {
                        og.Draw();
                    }
                }
            }
        }

        private class OrbitData
        {
            public Orbit orbit = new Orbit();
            public string type = null;
            public string name = null;
            public OrbitType orbitType = OrbitType.RANDOM;
            public double difficulty = 1.0;
            public int index = 0;

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
                name = orig.name;
                orbitType = orig.orbitType;
                difficulty = orig.difficulty;
                index = orig.index;

                // Lazy copy of orbit - only really used to story the orbital parameters, so not
                // a huge deal.
                orbit = orig.orbit;
            }
        }
        private List<OrbitData> orbits = new List<OrbitData>();

        protected static List<OrbitGenerator> allOrbitGenerators = new List<OrbitGenerator>();
        
        public OrbitGenerator() {}

        /*
         * Copy constructor.
         */
        public OrbitGenerator(OrbitGenerator orig, Contract contract)
            : base()
        {
            foreach (OrbitData old in orig.orbits)
            {
                // Copy orbit data
                orbits.Add(new OrbitData(old, contract));
            }

            System.Random random = new System.Random(contract.MissionSeed);

            // Register the orbit drawing class
            if (MapView.MapCamera.gameObject.GetComponent<OrbitRenderer>() == null)
            {
                MapView.MapCamera.gameObject.AddComponent<OrbitRenderer>();
            }

            // Find/add the AlwaysTrue parameter
            AlwaysTrue alwaysTrue = AlwaysTrue.FetchOrAdd(contract);

            int i = 0;
            foreach (OrbitData obData in orbits)
            {
                // Do type specific handling
                if (obData.type == "RANDOM_ORBIT")
                {
                    obData.orbit = CelestialUtilities.GenerateOrbit(obData.orbitType, contract.MissionSeed + i++, obData.orbit.referenceBody, obData.difficulty);
                }

                // Create the wrapper to the SpecificOrbit parameter that will do the rendering work
                SpecificOrbitWrapper s = new SpecificOrbitWrapper(obData.orbitType, obData.orbit.inclination,
                    obData.orbit.eccentricity, obData.orbit.semiMajorAxis, obData.orbit.LAN, obData.orbit.argumentOfPeriapsis,
                    obData.orbit.meanAnomalyAtEpoch, obData.orbit.epoch, obData.orbit.referenceBody,
                    obData.difficulty, 3.0);
                s.DisableOnStateChange = false;
                alwaysTrue.AddParameter(s);
                obData.index = alwaysTrue.ParameterCount - 1;
            }
        }

        public static OrbitGenerator Create(ConfigNode configNode, CelestialBody defaultBody, OrbitGeneratorFactory factory)
        {
            OrbitGenerator obGenerator = new OrbitGenerator();

            bool valid = true;
            foreach (ConfigNode child in configNode.GetNodes())
            {
                int count = child.HasValue("count") ? Convert.ToInt32(child.GetValue("count")) : 1;
                for (int i = 0; i < count; i++)
                {
                    OrbitData obData = new OrbitData(child.name);

                    // Get settings that differ by type
                    if (child.name == "FIXED_ORBIT")
                    {
                        valid &= ConfigNodeUtil.ValidateMandatoryChild(child, "ORBIT", factory);
                        obData.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();
                    }
                    else if (child.name == "RANDOM_ORBIT")
                    {
                        try
                        {
                            obData.orbitType = (OrbitType)Enum.Parse(typeof(OrbitType), child.GetValue("type"));
                        }
                        catch (Exception e)
                        {
                            valid = false;
                            LoggingUtil.LogError(typeof(OrbitGenerator), factory.ErrorPrefix(configNode) +
                                ": error parsing type: " + e.Message);
                        }
                        obData.difficulty = Convert.ToDouble(child.GetValue("difficulty"));
                    }
                    else
                    {
                        throw new ArgumentException("Unrecognized orbit node: '" + child.name + "'");
                    }

                    // Get target body
                    if (defaultBody == null)
                    {
                        valid &= ConfigNodeUtil.ValidateMandatoryField(child, "targetBody", factory);
                    }
                    obData.orbit.referenceBody = (child.HasValue("targetBody") ?
                        ConfigNodeUtil.ParseCelestialBody(child, "targetBody") : defaultBody);
                    
                    // Add to the list
                    obGenerator.orbits.Add(obData);
                }

                allOrbitGenerators.Add(obGenerator);
            }

            return valid ? obGenerator : null;
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            allOrbitGenerators.Remove(this);
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (ConfigNode child in configNode.GetNodes("ORBIT_DETAIL"))
            {
                // Read all the orbit data
                OrbitData obData = new OrbitData();
                obData.type = child.GetValue("type");
                obData.name = child.GetValue("name");
                obData.difficulty = Convert.ToDouble(child.GetValue("difficulty"));
                obData.index = Convert.ToInt32(child.GetValue("index"));

                obData.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();

                // Add to the global list
                orbits.Add(obData);
            }

            allOrbitGenerators.Add(this);
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (OrbitData obData in orbits)
            {
                ConfigNode child = new ConfigNode("ORBIT_DETAIL");

                child.AddValue("type", obData.type);
                child.AddValue("name", obData.name);
                child.AddValue("difficulty", obData.difficulty);
                child.AddValue("index", obData.index);

                ConfigNode orbitNode = new ConfigNode("ORBIT");
                new OrbitSnapshot(obData.orbit).Save(orbitNode);
                child.AddNode(orbitNode);

                configNode.AddNode(child);
            }
        }

        public void Draw()
        {
            // No contract
            if (contract == null)
            {
                return;
            }

            // Check contract state when displaying
            if (contract.ContractState == Contract.State.Active ||
                contract.ContractState == Contract.State.Offered && HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                // Update the map icons
                foreach (OrbitData obData in orbits)
                {
                    SpecificOrbitParameter s = AlwaysTrue.FetchOrAdd(contract).GetParameter(obData.index) as SpecificOrbitParameter;
                    s.updateMapIcons(CelestialUtilities.MapFocusBody() == obData.orbit.referenceBody);
                }
            }
        }

        public SpecificOrbitWrapper GetOrbitParameter(int index)
        {
            return AlwaysTrue.FetchOrAdd(contract).GetParameter(orbits[index].index) as SpecificOrbitWrapper;
        }
    }
}
