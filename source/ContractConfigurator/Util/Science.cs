using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;

namespace ContractConfigurator.Util
{
    public static class Science
    {
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

        private static IEnumerable<ScienceSubject> GetSubjects(ScienceExperiment experiment, CelestialBody body, Func<string, bool> biomeFilter)
        {
            IEnumerable<ExperimentSituations> situations = Enum.GetValues(typeof(ExperimentSituations)).Cast<ExperimentSituations>();

            bool evaCheckRequired = !GameVariables.Instance.UnlockedEVA(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex)) &&
                (experiment.id == "surfaceSample" || experiment.id == "evaReport");

            return situations
                .Where(sit => experiment.IsAvailableWhile(sit, body) &&
                    (sit != ExperimentSituations.SrfSplashed || body.ocean) &&
                    ((sit != ExperimentSituations.FlyingLow && sit != ExperimentSituations.FlyingHigh) || body.atmosphere) &&
                    (!evaCheckRequired || sit == ExperimentSituations.SrfLanded || sit == ExperimentSituations.SrfSplashed))
                .SelectMany(sit =>
                {
                    var biomesPlusKsc = (experiment.BiomeIsRelevantWhile(sit)
                        ? ResearchAndDevelopment.GetBiomeTags(body).ToArray()
                        : Enumerable.Empty<string>()).Where(biomeFilter).ToList();

                    var biomes =
                        biomesPlusKsc.Where(
                            biome =>
                                body.BiomeMap != null && body.BiomeMap.Attributes.Any(attr => attr.name.Replace(" ", string.Empty) == biome))
                                .ToList();

                    var kscStatics = biomesPlusKsc.Except(biomes);

                    return (biomesPlusKsc.Any() ? biomes : new List<string> { string.Empty })
                        .Select(biome => ScienceSubject(experiment, sit, body, biome))
                        .Union(sit == ExperimentSituations.SrfLanded // static KSC items can only be landed on as far as I know
                            ? kscStatics.Select(
                                staticName =>
                                    ScienceSubject(experiment, ExperimentSituations.SrfLanded, body, staticName))
                                    : Enumerable.Empty<ScienceSubject>());
                });
        }

        public static IEnumerable<ScienceSubject> GetSubjects(IEnumerable<CelestialBody> celestialBodies, Func<ScienceExperiment, bool> experimentFilter = null,
            Func<string, bool> biomeFilter = null)
        {
            if (ResearchAndDevelopment.Instance == null)
            {
                yield break;
            }

            // Get all the experiments
            IEnumerable<ScienceExperiment> experiments = ResearchAndDevelopment.GetExperimentIDs().
                Select<string, ScienceExperiment>(ResearchAndDevelopment.GetExperiment);

            // Filter out asteroid samples if not unlocked
            bool asteroidTracking = GameVariables.Instance.UnlockedSpaceObjectDiscovery(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation));
            experiments = experiments.Where(exp => exp.id != "asteroidSample" || asteroidTracking);

            // Build a mapping of experiment => parts
            Dictionary<string, List<AvailablePart>> experimentParts = new Dictionary<string, List<AvailablePart>>();
            foreach (KeyValuePair<AvailablePart, string> pair in PartLoader.Instance.parts.Where(p => p.moduleInfos.Any(mod => mod.moduleName == "Science Experiment")).
                SelectMany(p => p.partConfig.GetNodes("MODULE").Where(node => node.GetValue("name") == "ModuleScienceExperiment").Select(node => new KeyValuePair<AvailablePart, string>(p, node.GetValue("experimentID")))))
            {
                if (!experimentParts.ContainsKey(pair.Value))
                {
                    experimentParts[pair.Value] = new List<AvailablePart>();
                }
                experimentParts[pair.Value].Add(pair.Key);
            }

            // Filter out anything tied to a part that isn't unlocked
            experiments = experiments.Where(exp => !experimentParts.ContainsKey(exp.id) || experimentParts[exp.id].Any(ResearchAndDevelopment.PartTechAvailable));

            // Unlocked surface samples/EVA
            bool surfaceSampleUnlocked = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) >= 0.5f;
            bool evaUnlocked = GameVariables.Instance.UnlockedEVA(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex));

            // Filter experiments
            if (experimentFilter != null)
            {
                experiments = experiments.Where(experimentFilter);
            }

            // Set up the biome filter
            if (biomeFilter == null)
            {
                biomeFilter = new Func<string, bool>(x => true);
            }

            // Return subjects for each celestial body
            foreach (CelestialBody body in celestialBodies.Where(cb => cb != null))
            {
                foreach (ScienceExperiment experiment in experiments.Where(exp =>
                    (exp.id != "surfaceSample" || (surfaceSampleUnlocked && (body.isHomeWorld || evaUnlocked))) &&
                    (exp.id != "evaReport" || (body.isHomeWorld || evaUnlocked))
                    ))
                {
                    foreach (ScienceSubject subject in GetSubjects(experiment, body, biomeFilter))
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
                    return (ExperimentSituations)Enum.Parse(typeof(ExperimentSituations), sitAndBiome);
                }
                catch
                {
                    m = Regex.Match(sitAndBiome, @"(.*)[A-Z][\w]+$");
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

            Match m = Regex.Match(subject.id, @"@([A-Z][\w]+?)([A-Z].*)");
            string celestialBody = m.Groups[1].Value;

            return ConfigNodeUtil.ParseCelestialBodyValue(celestialBody);

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
                    Enum.Parse(typeof(ExperimentSituations), sitAndBiome);
                    break;
                }
                catch
                {
                    m = Regex.Match(sitAndBiome, @"(.*)([A-Z][\w]+)$");
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

    }
}
