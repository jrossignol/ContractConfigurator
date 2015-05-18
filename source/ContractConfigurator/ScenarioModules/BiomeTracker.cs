using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    public class BiomeTracker : ScenarioModule
    {
        private class BiomeData
        {
            public string name;
            public int landCount;
            public int waterCount;

            public double landRatio
            {
                get
                {
                    return (double)landCount / (landCount + waterCount);
                }
            }

            public List<Vector2d> landLocations = new List<Vector2d>();
            public List<Vector2d> waterLocations = new List<Vector2d>();

            public BiomeData(string name)
            {
                this.name = name;
            }

            public void Save(ConfigNode node)
            {
                node.AddValue("name", name);
                node.AddValue("landCount", landCount);
                node.AddValue("waterCount", waterCount);

                foreach (Vector2d v in landLocations)
                {
                    ConfigNode location = new ConfigNode("LAND_LOCATION");
                    node.AddNode(location);
                    location.AddValue("lat", v.y);
                    location.AddValue("lon", v.x);
                }

                foreach (Vector2d v in waterLocations)
                {
                    ConfigNode location = new ConfigNode("WATER_LOCATION");
                    node.AddNode(location);
                    location.AddValue("lat", v.y);
                    location.AddValue("lon", v.x);
                }
            }

            public static BiomeData Load(ConfigNode node)
            {
                BiomeData biomeData = new BiomeData(ConfigNodeUtil.ParseValue<string>(node, "name"));
                biomeData.landCount = ConfigNodeUtil.ParseValue<int>(node, "landCount");
                biomeData.waterCount = ConfigNodeUtil.ParseValue<int>(node, "waterCount");

                foreach (ConfigNode landLocation in node.GetNodes("LAND_LOCATION"))
                {
                    Vector2d v = new Vector2d();
                    v.y = ConfigNodeUtil.ParseValue<double>(landLocation, "lat");
                    v.x = ConfigNodeUtil.ParseValue<double>(landLocation, "lon");
                    biomeData.landLocations.Add(v);
                }

                foreach (ConfigNode landLocation in node.GetNodes("WATER_LOCATION"))
                {
                    Vector2d v = new Vector2d();
                    v.y = ConfigNodeUtil.ParseValue<double>(landLocation, "lat");
                    v.x = ConfigNodeUtil.ParseValue<double>(landLocation, "lon");
                    biomeData.landLocations.Add(v);
                }

                return biomeData;
            }
        }
        private static BiomeTracker Instance;
        private Dictionary<CelestialBody, Dictionary<string, BiomeData>> bodyInfo = new Dictionary<CelestialBody, Dictionary<string, BiomeData>>();

        private bool loaded = false;

        void Start()
        {
            DontDestroyOnLoad(this);
            Instance = this;

            StartCoroutine(LoadAllBodyInfo());
        }

        void Destroy()
        {
        }

        void Update()
        {
            // Load all the contract configurator configuration
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !loaded)
            {
                loaded = true;
            }
        }

        IEnumerator<YieldInstruction> LoadAllBodyInfo()
        {
            foreach (YieldInstruction ins in FlightGlobals.Bodies.SelectMany<CelestialBody, YieldInstruction>(LoadBodyInfo))
            {
                yield return ins;
            }
        }

        IEnumerable<YieldInstruction> LoadBodyInfo(CelestialBody body)
        {
            if (body == null || body.pqsController == null || !body.ocean || bodyInfo.ContainsKey(body))
            {
                yield break;
            }

            Dictionary<string, BiomeData> biomeData = new Dictionary<string, BiomeData>();

            int biomeCount = body.BiomeMap.Attributes.Length;
            float startTime = Time.realtimeSinceStartup;
            float timeStep = 0.01f;
            int maxCount = 1000;

            int w = 4096;
            int h = 2048;
            int bu = 0;
            int bv = 0;

            LoggingUtil.LogInfo(this, "Starting background load of " + body.name + " biome data.");
            int count = 0;
            for (int i = 0; i < w; i++)
            {
                bu = (bu + 977) % w;

                double lonRads = 2.0 * Math.PI * ((bu + 0.5) / w);
                double cosLon = Math.Cos(lonRads);
                double sinLon = Math.Sin(lonRads);

                for (int j = 0; j < h; j++)
                {
                    count++;
                    bv = (bv + 239) % h;

                    double latRads = Math.PI * (0.5 - (bv + 0.5) / h);
                    double cosLat = Math.Cos(latRads);
                    double sinLat = Math.Sin(latRads);

                    // Get biome data
                    string biome = body.BiomeMap.GetAtt(latRads, lonRads).name;
                    BiomeData bd;
                    biomeData.TryGetValue(biome, out bd);
                    if (bd == null)
                    {
                        bd = biomeData[biome] = new BiomeData(biome);
                    }

                    if (bd.landCount + bd.waterCount < maxCount || bd.landLocations.Count < 3 || bd.waterLocations.Count < 3)
                    {
                        Vector3d radialVector = new Vector3d(cosLat * cosLon, sinLat, cosLat * sinLon);
                        double height = body.pqsController.GetSurfaceHeight(radialVector) - body.pqsController.radius;

                        if (height > 0.0)
                        {
                            bd.landCount++;
                            if (bd.landLocations.Count < 3)
                            {
                                double lon = lonRads * 180.0 / Math.PI;
                                double lat = latRads * 180.0 / Math.PI;
                                if (!bd.landLocations.Any(v => Math.Abs(v.x - lon) < 2.0 || Math.Abs(v.y - lat) < 2.0))
                                {
                                    bd.landLocations.Add(new Vector2d(lon, latRads * 180.0 / Math.PI));
                                }
                            }
                        }
                        else
                        {
                            bd.waterCount++;
                            if (bd.waterLocations.Count < 3)
                            {
                                double lon = lonRads * 180.0 / Math.PI;
                                double lat = latRads * 180.0 / Math.PI;
                                if (!bd.waterLocations.Any(v => Math.Abs(v.x - lon) < 2.0 || Math.Abs(v.y - lat) < 2.0))
                                {
                                    bd.waterLocations.Add(new Vector2d(lon, latRads * 180.0 / Math.PI));
                                }
                            }
                        }
                    }

                    // Take a break
                    if (Time.realtimeSinceStartup >= startTime + timeStep)
                    {
                        yield return null;
                        startTime = Time.realtimeSinceStartup;
                    }
                }

                // Check for completion after every "row"
                if (biomeData.Count == biomeCount &&
                    biomeData.All(pair => pair.Value.landCount + pair.Value.waterCount >= maxCount &&
                        pair.Value.landLocations.Count >= 3 && pair.Value.waterLocations.Count >= 3))
                {
                    break;
                }
            }

            // Build a color => name map
            Dictionary<string, string> nameMap = new Dictionary<string, string>();
            for (int i = 0; i < body.BiomeMap.Attributes.Length; i++)
            {
                nameMap[body.BiomeMap.Attributes[i].mapColor.ToString()] = body.BiomeMap.Attributes[i].name;
			}

            // Save the biomData that was collected
            bodyInfo[body] = biomeData;

            LoggingUtil.LogInfo(this, "Completed background load of " + body.name + " biome data.");
        }

        public override void OnLoad(ConfigNode node)
        {
            foreach (ConfigNode bodyNode in node.GetNodes("CELESTIAL_BODY"))
            {
                CelestialBody body = ConfigNodeUtil.ParseValue<CelestialBody>(bodyNode, "body");
                Dictionary<string, BiomeData> biomeDetails = bodyInfo[body] = new Dictionary<string, BiomeData>();

                foreach (ConfigNode biomeNode in bodyNode.GetNodes("BIOME"))
                {
                    BiomeData biomeData = BiomeData.Load(biomeNode);
                    biomeDetails.Add(biomeData.name, biomeData);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            foreach (KeyValuePair<CelestialBody, Dictionary<string, BiomeData>> pair in bodyInfo)
            {
                ConfigNode bodyNode = new ConfigNode("CELESTIAL_BODY");
                node.AddNode(bodyNode);
                bodyNode.AddValue("body", pair.Key.name);

                foreach (BiomeData biomeData in pair.Value.Values)
                {
                    ConfigNode biomeNode = new ConfigNode("BIOME");
                    bodyNode.AddNode(biomeNode);
                    biomeData.Save(biomeNode);
                }
            }
        }

        public static bool IsDifficult(CelestialBody body, string biome, ExperimentSituations situation)
        {
            if (body == null || !body.ocean || body.pqsController == null ||
                (situation != ExperimentSituations.SrfLanded && situation != ExperimentSituations.SrfSplashed))
            {
                return false;
            }

            if (Instance == null || !Instance.bodyInfo.ContainsKey(body))
            {
                return true;
            }

            // Handles KSC biomes
            if (!Instance.bodyInfo[body].ContainsKey(biome))
            {
                return situation == ExperimentSituations.SrfSplashed;
            }

            double landRatio = Instance.bodyInfo[body][biome].landRatio;
            return landRatio > 0.95 && situation == ExperimentSituations.SrfSplashed ||
                landRatio < 0.05 && situation == ExperimentSituations.SrfLanded;
        }

        public static IEnumerable<Vector2d> GetDifficultLocations(CelestialBody body, string biome)
        {
            if (body == null || Instance == null || !Instance.bodyInfo.ContainsKey(body))
            {
                yield break;
            }

            double landRatio = Instance.bodyInfo[body][biome].landRatio;
            List<Vector2d> list = landRatio > 0.95 ? Instance.bodyInfo[body][biome].waterLocations :
                landRatio < 0.05 ? Instance.bodyInfo[body][biome].waterLocations : new List<Vector2d>();
            foreach (Vector2d v in list)
            {
                yield return v;
            }
        }
    }
}
