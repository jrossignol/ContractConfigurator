using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using KSP.Localization;

namespace ContractConfigurator.Util
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class Science : MonoBehaviour
    {
        private class ExperimentRules
        {
            public string id;
            public bool ignored;
            public bool requireEVA;
            public bool requireSurfaceSample;
            public bool requireAsteroidTracking;
            public bool requireAtmosphere;
            public bool requireNoAtmosphere;
            public bool requireSurface;
            public bool requireNoSurface;
            public bool disallowHomeSurface;
            public bool disallowHomeFlying;
            public bool disallowKSC;
            public bool partless;
            public bool sunOnly;
            public string partModule;
            public List<string> part;

            public ExperimentRules(string id)
            {
                this.id = id;
            }
        }
        private static Dictionary<string, ExperimentRules> experimentRules = new Dictionary<string, ExperimentRules>();
        private static List<string> experimentModules = new List<string>();

        public Science Instance;
        private bool loaded = false;

        private static Dictionary<string, List<AvailablePart>> experimentParts = null;
        private static Dictionary<string, bool> partlessExperiments = new Dictionary<string, bool>();
        private static IEnumerable<ExperimentSituations> allSituations = Enum.GetValues(typeof(ExperimentSituations)).OfType<ExperimentSituations>();

        void Start()
        {
            DontDestroyOnLoad(this);
            Instance = this;
        }

        void Update()
        {
            if (!loaded && ResearchAndDevelopment.Instance != null)
            {
                Load();
            }
        }

        private void Load()
        {
            ConfigNode[] experimentConfigs = GameDatabase.Instance.GetConfigNodes("CC_EXPERIMENT_DEFINITIONS");

            foreach (ConfigNode experimentConfig in experimentConfigs)
            {
                LoggingUtil.LogDebug(this, "Loading experiment definitions for {0}", experimentConfig.GetValue("name"));

                foreach (ConfigNode config in experimentConfig.GetNodes("EXPERIMENT"))
                {
                    string name = ConfigNodeUtil.ParseValue<string>(config, "name");
                    LoggingUtil.LogVerbose(this, "    loading experiment {0}", name);

                    ExperimentRules exp = new ExperimentRules(name);
                    experimentRules[name] = exp;

                    exp.ignored = ConfigNodeUtil.ParseValue<bool?>(config, "ignored", (bool?)false).Value;
                    exp.requireEVA = ConfigNodeUtil.ParseValue<bool?>(config, "requireEVA", (bool?)false).Value;
                    exp.requireSurfaceSample = ConfigNodeUtil.ParseValue<bool?>(config, "requireSurfaceSample", (bool?)false).Value;
                    exp.requireAsteroidTracking = ConfigNodeUtil.ParseValue<bool?>(config, "requireAsteroidTracking", (bool?)false).Value;
                    exp.requireAtmosphere = ConfigNodeUtil.ParseValue<bool?>(config, "requireAtmosphere", (bool?)false).Value;
                    exp.requireNoAtmosphere = ConfigNodeUtil.ParseValue<bool?>(config, "requireNoAtmosphere", (bool?)false).Value;
                    exp.requireSurface = ConfigNodeUtil.ParseValue<bool?>(config, "requireSurface", (bool?)false).Value;
                    exp.requireNoSurface = ConfigNodeUtil.ParseValue<bool?>(config, "requireNoSurface", (bool?)false).Value;
                    exp.disallowHomeSurface = ConfigNodeUtil.ParseValue<bool?>(config, "disallowHomeSurface", (bool?)false).Value;
                    exp.disallowHomeFlying = ConfigNodeUtil.ParseValue<bool?>(config, "disallowHomeFlying", (bool?)false).Value;
                    exp.disallowKSC = ConfigNodeUtil.ParseValue<bool?>(config, "disallowKSC", (bool?)false).Value;
                    exp.partless = ConfigNodeUtil.ParseValue<bool?>(config, "partless", (bool?)false).Value;
                    exp.part = ConfigNodeUtil.ParseValue<List<string>>(config, "part", null);
                    exp.partModule = ConfigNodeUtil.ParseValue<string>(config, "partModule", null);
                    exp.sunOnly = ConfigNodeUtil.ParseValue<bool?>(config, "sunOnly", (bool?)false).Value;
                }

                // Add the experiment modules
                foreach (ConfigNode config in experimentConfig.GetNodes("MODULE"))
                {
                    string name = ConfigNodeUtil.ParseValue<string>(config, "name");
                    LoggingUtil.LogVerbose(this, "    loading module {0}", name);

                    experimentModules.Add(name);
                }
            }

            // Add experiment modules based on subclassing
            foreach (Type expModule in ContractConfigurator.GetAllTypes<ModuleScienceExperiment>())
            {
                LoggingUtil.LogVerbose(this, "    adding module for class {0}", expModule.Name);
                experimentModules.AddUnique(expModule.Name);
            }

            loaded = true;
        }

        /// <summary>
        /// Gets the science subject for the given values.
        /// </summary>
        /// <param name="experiment">The science experiment</param>
        /// <param name="situation">The experimental situation</param>
        /// <param name="body">The celestial body</param>
        /// <param name="biome">The biome</param>
        /// <returns>The ScienceSubject</returns>
        public static ScienceSubject ScienceSubject(ScienceExperiment experiment, ExperimentSituations situation, CelestialBody body, string biome)
        {
            ScienceSubject defaultIfNotResearched = new ScienceSubject(experiment, situation, body, biome);

            return ResearchAndDevelopment.GetSubjects().SingleOrDefault(researched => defaultIfNotResearched.id == researched.id) ?? defaultIfNotResearched;
        }

        private static IEnumerable<ScienceSubject> GetSubjects(ScienceExperiment experiment, CelestialBody body, Func<string, bool> biomeFilter, bool difficult)
        {
            IEnumerable<ExperimentSituations> situations = Enum.GetValues(typeof(ExperimentSituations)).Cast<ExperimentSituations>();

            // Set up the biome filter
            bool biomesFiltered = biomeFilter != null;
            if (biomeFilter == null)
            {
                biomeFilter = new Func<string, bool>(x => true);
            }

            IEnumerable<string> biomes = body.BiomeMap == null ? Enumerable.Empty<string>() :
                body.BiomeMap.Attributes.Select(attr => attr.name.Replace(" ", string.Empty)).
                Where(biomeFilter);

            return situations
                .Where(sit => ExperimentAvailable(experiment, sit, body) &&
                    (sit != ExperimentSituations.SrfSplashed || body.ocean) &&
                    ((sit != ExperimentSituations.FlyingLow && sit != ExperimentSituations.FlyingHigh) || body.atmosphere))
                .SelectMany<ExperimentSituations, ScienceSubject>(sit =>
                {
                    if (experiment.BiomeIsRelevantWhile(sit))
                    {
                        ExperimentRules rules = GetExperimentRules(experiment.id);

                        return biomes.Where(biome => !(BiomeTracker.IsDifficult(body, biome, sit) || experiment.id == "asteroidSample") ^ difficult)
                            .Select(biome => ScienceSubject(experiment, sit, body, biome))
                            .Union(body.isHomeWorld && !rules.disallowKSC && sit == ExperimentSituations.SrfLanded // static KSC items can only be landed
                                ? Biome.KSCBiomes.Where(biomeFilter).Where(b => experiment.id == "asteroidSample" ^ !difficult).Select(
                                    staticName =>
                                        ScienceSubject(experiment, ExperimentSituations.SrfLanded, body, staticName))
                                        : Enumerable.Empty<ScienceSubject>());
                    }
                    else if (experiment.id.StartsWith("ROCScience") && biomesFiltered)
                    {
                        ROCDefinition roc = ROCManager.Instance.rocDefinitions.Where(r => r.myCelestialBodies.Any(x => x.name == body.name) && experiment.id.Contains(r.type)).FirstOrDefault();
                        if (roc != null && roc.myCelestialBodies.First().biomes.Where(biomeFilter).Any())
                        {
                            return new ScienceSubject[] { ScienceSubject(experiment, sit, body, "") };
                        }
                        else
                        {
                            return Enumerable.Empty<ScienceSubject>();
                        }
                    }
                    else if (!biomesFiltered && !difficult)
                    {
                        return new ScienceSubject[] { ScienceSubject(experiment, sit, body, "") };
                    }
                    else
                    {
                        return Enumerable.Empty<ScienceSubject>();
                    }
                });
        }

        public static IEnumerable<ScienceSubject> GetSubjects(IEnumerable<CelestialBody> celestialBodies, Func<ScienceExperiment, bool> experimentFilter = null,
            Func<string, bool> biomeFilter = null, bool difficult = false)
        {
            if (ResearchAndDevelopment.Instance == null || celestialBodies == null)
            {
                yield break;
            }

            // Get all the available experiments
            IEnumerable<ScienceExperiment> experiments = AvailableExperiments();

            // Filter experiments
            if (experimentFilter != null)
            {
                experiments = experiments.Where(experimentFilter);
            }

            // Return subjects for each celestial body
            foreach (CelestialBody body in celestialBodies.Where(cb => cb != null))
            {
                foreach (ScienceExperiment experiment in experiments)
                {
                    foreach (ScienceSubject subject in GetSubjects(experiment, body, biomeFilter, difficult))
                    {
                        yield return subject;
                    }
                }
            }
        }

        public static ScienceExperiment GetExperiment(ScienceSubject subject)
        {
            if (subject == null || ResearchAndDevelopment.Instance == null)
            {
                return null;
            }

            return ResearchAndDevelopment.GetExperiment(subject.id.Substring(0, subject.id.IndexOf("@")));
        }

        public static ExperimentSituations GetSituation(ScienceSubject subject)
        {
            if (subject == null || ResearchAndDevelopment.Instance == null)
            {
                return ExperimentSituations.SrfLanded;
            }

            Match m = Regex.Match(subject.id, @"@[A-Z][\w]+?([A-Z].*)");
            string sitAndBiome = m.Groups[1].Value;

            while (!string.IsNullOrEmpty(sitAndBiome))
            {
                try
                {
                    return (ExperimentSituations)Enum.Parse(typeof(ExperimentSituations), sitAndBiome, true);
                }
                catch
                {
                    m = Regex.Match(sitAndBiome, @"(.*)[A-Z][\w]*$");
                    sitAndBiome = m.Groups[1].Value;
                }
            }

            return ExperimentSituations.SrfLanded;
        }

        public static CelestialBody GetCelestialBody(ScienceSubject subject)
        {
            if (subject == null || ResearchAndDevelopment.Instance == null)
            {
                return null;
            }

            string celestialBody;
            if (subject.id.StartsWith("ROCScience"))
            {
                Match m = Regex.Match(subject.id, @"ROCScience_([A-Z][\w]+?)([A-Z].*)");
                celestialBody = m.Groups[1].Value;
            }
            else
            {
                Match m = Regex.Match(subject.id, @"@([A-Z][\w]+?)([A-Z].*)");
                celestialBody = m.Groups[1].Value;
            }

            return string.IsNullOrEmpty(celestialBody) ? null : ConfigNodeUtil.ParseCelestialBodyValue(celestialBody);
        }

        public static Biome GetBiome(ScienceSubject subject)
        {
            if (subject == null || ResearchAndDevelopment.Instance == null)
            {
                return null;
            }

            Match m = Regex.Match(subject.id, @"@([A-Z][\w]+?)([A-Z].*)");
            string celestialBody = m.Groups[1].Value;
            string sitAndBiome = m.Groups[2].Value;

            string biome = "";
            while (!string.IsNullOrEmpty(sitAndBiome))
            {
                try
                {
                    Enum.Parse(typeof(ExperimentSituations), sitAndBiome, true);
                    break;
                }
                catch
                {
                    m = Regex.Match(sitAndBiome, @"(.*)([A-Z][\w&]*)$$");
                    sitAndBiome = m.Groups[1].Value;
                    biome = m.Groups[2].Value + biome;
                }
            }

            return string.IsNullOrEmpty(biome) ? null : new Biome(ConfigNodeUtil.ParseCelestialBodyValue(celestialBody), biome);
        }


        public static float NextScienceReportValue(ScienceSubject subject)
        {
            if (ResearchAndDevelopment.Instance == null || HighLogic.CurrentGame == null || subject == null)
            {
                return 0.0f;
            }

            ScienceExperiment experiment = GetExperiment(subject);

            return ResearchAndDevelopment.GetScienceValue(
                experiment.baseValue * experiment.dataScale,
                subject) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
        }

        private static bool ExperimentAvailable(ScienceExperiment exp, CelestialBody body)
        {
            if (exp == null || body == null)
            {
                return false;
            }

            // Check if experiement is unlocked
            if (!exp.IsUnlocked())
            {
                return false;
            }

            // Special Breaking Ground logic
            if (exp.id.StartsWith("ROCScience"))
            {
                if (!exp.id.Contains(body.name))
                {
                    return false;
                }
            }

            // Get the experiment rules
            ExperimentRules rules = GetExperimentRules(exp.id);

            if (rules.ignored)
            {
                return false;
            }

            if ((rules.requireAtmosphere || exp.requireAtmosphere) && !body.atmosphere)
            {
                return false;
            }

            if ((rules.requireNoAtmosphere || exp.requireNoAtmosphere) && body.atmosphere)
            {
                return false;
            }

            if (rules.requireSurface && body.pqsController == null)
            {
                return false;
            }

            if (rules.requireNoSurface && body.pqsController != null)
            {
                return false;
            }

            if (rules.sunOnly)
            {
                return body == FlightGlobals.Bodies[0];
            }

            // Filter out asteroid samples if not unlocked
            if (rules.requireAsteroidTracking)
            {
                if (!GameVariables.Instance.UnlockedSpaceObjectDiscovery(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation)))
                {
                    return false;
                }
            }

            return allSituations.Any(sit => exp.IsAvailableWhile(sit, body));
        }

        private static bool ExperimentAvailable(ScienceExperiment exp, ExperimentSituations sit, CelestialBody body)
        {
            if (!ExperimentAvailable(exp, body))
            {
                return false;
            }

            if (!exp.IsAvailableWhile(sit, body))
            {
                return false;
            }

            // Get the experiment rules
            ExperimentRules rules = GetExperimentRules(exp.id);

            // Check if surface samples have been unlocked
            if (rules.requireSurfaceSample)
            {
                if (ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) < 0.3f)
                {
                    return false;
                }
            }

            // Check for EVA unlock
            if (rules.requireEVA)
            {
                if (!body.isHomeWorld || (sit != ExperimentSituations.SrfLanded && sit != ExperimentSituations.SrfSplashed))
                {
                    bool evaUnlocked = GameVariables.Instance.UnlockedEVA(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex));
                    if (!evaUnlocked)
                    {
                        return false;
                    }
                }
            }

            if (rules.disallowHomeSurface)
            {
                if (body.isHomeWorld && sit == ExperimentSituations.SrfLanded || sit == ExperimentSituations.SrfSplashed)
                {
                    return false;
                }
            }

            if (rules.disallowHomeFlying)
            {
                if (body.isHomeWorld && sit == ExperimentSituations.FlyingLow || sit == ExperimentSituations.FlyingHigh)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets an enumeration of all available experiments
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ScienceExperiment> AvailableExperiments()
        {
            if (ResearchAndDevelopment.Instance == null)
            {
                return Enumerable.Empty<ScienceExperiment>();
            }

            IEnumerable<ScienceExperiment> experiments = ResearchAndDevelopment.GetExperimentIDs().Select<string, ScienceExperiment>(ResearchAndDevelopment.GetExperiment);

            // Build a mapping of experiment => parts
            if (experimentParts == null)
            {
                experimentParts = new Dictionary<string, List<AvailablePart>>();

                // Check the stock experiment
                foreach (KeyValuePair<AvailablePart, string> pair in PartLoader.Instance.loadedParts.
                    Where(p => p.partConfig != null).
                    SelectMany(p => p.partConfig.GetNodes("MODULE").
                        Where(node => experimentModules.Contains(node.GetValue("name"))).
                        Select(node => new KeyValuePair<AvailablePart, string>(p, node.GetValue("experimentID") ?? node.GetValue("experimentId")))))
                {
                    if (!string.IsNullOrEmpty(pair.Value))
                    {
                        if (!experimentParts.ContainsKey(pair.Value))
                        {
                            experimentParts[pair.Value] = new List<AvailablePart>();
                        }
                        experimentParts[pair.Value].Add(pair.Key);
                    }
                }

                // Check for specific modules specified in configurator
                foreach (ExperimentRules rules in experiments.Select(exp => GetExperimentRules(exp.id)).Where(r => !string.IsNullOrEmpty(r.partModule)))
                {
                    if (!experimentParts.ContainsKey(rules.id))
                    {
                        experimentParts[rules.id] = new List<AvailablePart>();
                    }

                    string module = rules.partModule;
                    foreach (AvailablePart p in PartLoader.Instance.loadedParts.Where(p => p.moduleInfos.Any(mod => mod.moduleName == module)))
                    {
                        LoggingUtil.LogVerbose(typeof(Science), "Adding entry for {0} = {1}", rules.id, p.name);
                        experimentParts[rules.id].Add(p);
                    }
                }

                // Add part-specific rules
                foreach (ExperimentRules rules in experiments.Select(exp => GetExperimentRules(exp.id)).Where(r => r.part != null))
                {
                    if (!experimentParts.ContainsKey(rules.id))
                    {
                        experimentParts[rules.id] = new List<AvailablePart>();
                    }

                    foreach (string pname in rules.part)
                    {
                        foreach (AvailablePart p in PartLoader.Instance.loadedParts.Where(p => p.name == pname))
                        {
                            experimentParts[rules.id].Add(p);
                        }
                    }
                }

                // List out the partless experiments
                foreach (ScienceExperiment exp in experiments.Where(exp => GetExperimentRules(exp.id).partless))
                {
                    partlessExperiments[exp.id] = true;
                }
            }

            // Filter out anything tied to a part that isn't unlocked
            experiments = experiments.Where(exp => partlessExperiments.ContainsKey(exp.id) ||
                experimentParts.ContainsKey(exp.SummaryID()) && experimentParts[exp.SummaryID()].Any(ResearchAndDevelopment.PartTechAvailable));

            return experiments;
        }

        /// <summary>
        /// Gets an enumeration of all available experiments
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ScienceExperiment> AvailableExperiments(CelestialBody body)
        {
            if (ResearchAndDevelopment.Instance == null)
            {
                return Enumerable.Empty<ScienceExperiment>();
            }

            return AvailableExperiments().Where(exp => ExperimentAvailable(exp, body));
        }

        private static ExperimentRules GetExperimentRules(string id)
        {
            // Get the experiment rules
            if (!experimentRules.ContainsKey(id))
            {
                if (!id.StartsWith("ROCScience_"))
                {
                    LoggingUtil.LogWarning(typeof(Science), "Experiment '{0}' is unknown, assuming a standard experiment.", id);
                }
                experimentRules[id] = new ExperimentRules(id);
            }
            return experimentRules[id];
        }
    }

    /// <summary>
    /// Adds some print methods.
    /// </summary>
    public static class ScienceExtensions
    {
        public static string Print(this ExperimentSituations exp)
        {
            switch (exp)
            {
                case ExperimentSituations.FlyingHigh:
                    return Localizer.Format("<<zC:1>>", Localizer.GetStringByTag("#autoLOC_6002003"));
                case ExperimentSituations.FlyingLow:
                    return Localizer.Format("<<zC:1>>", Localizer.GetStringByTag("#autoLOC_6002002"));
                case ExperimentSituations.InSpaceHigh:
                    return Localizer.GetStringByTag("#cc.science.sit.InSpaceHigh");
                case ExperimentSituations.InSpaceLow:
                    return Localizer.GetStringByTag("#cc.science.sit.InSpaceLow");
                case ExperimentSituations.SrfLanded:
                    return Localizer.GetStringByTag("#autoLOC_268855");
                case ExperimentSituations.SrfSplashed:
                    return Localizer.GetStringByTag("#autoLOC_268858");
                default:
                    throw new ArgumentException("Unexpected experiment situation: " + exp);
            }
        }

        public static string SummaryID(this ScienceExperiment exp)
        {
            if (exp.id.StartsWith("ROCScience"))
            {
                return "ROCScience";
            }
            else
            {
                return exp.id;
            }
        }
    }
}
