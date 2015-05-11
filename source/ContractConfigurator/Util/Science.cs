using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static IEnumerable<ScienceSubject> GetSubjects(ScienceExperiment experiment, CelestialBody body)
        {
            IEnumerable<ExperimentSituations> situations = Enum.GetValues(typeof(ExperimentSituations)).Cast<ExperimentSituations>();

            return situations
                .Where(sit => experiment.IsAvailableWhile(sit, body) && (sit != ExperimentSituations.SrfSplashed || body.ocean))
                .SelectMany(sit =>
                {
                    var biomesPlusKsc = (experiment.BiomeIsRelevantWhile(sit)
                        ? ResearchAndDevelopment.GetBiomeTags(body).ToArray()
                        : Enumerable.Empty<string>()).ToList();

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


        public static IEnumerable<ScienceSubject> GetSubjects(IEnumerable<CelestialBody> celestialBodies, Func<ScienceExperiment, bool> experimentFilter = null)
        {
            if (ResearchAndDevelopment.Instance == null)
            {
                yield break;
            }

            // Get all the experiments
            IEnumerable<ScienceExperiment> experiments = ResearchAndDevelopment.GetExperimentIDs().
                Select<string, ScienceExperiment>(ResearchAndDevelopment.GetExperiment);

            // Filter experiments
            if (experimentFilter != null)
            {
                experiments = experiments.Where(experimentFilter);
            }

            // Return subjects for each celestial body
            foreach (CelestialBody body in celestialBodies)
            {
                foreach (ScienceExperiment experiment in experiments)
                {
                    foreach (ScienceSubject subject in GetSubjects(experiment, body))
                    {
                        yield return subject;
                    }
                }
            }
        }
    }
}
