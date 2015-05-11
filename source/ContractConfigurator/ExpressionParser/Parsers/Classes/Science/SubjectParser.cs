using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using ContractConfigurator.Util;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Biome.
    /// </summary>
    public class SubjectParser : ClassExpressionParser<ScienceSubject>, IExpressionParserRegistrer
    {
        static SubjectParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(ScienceSubject), typeof(SubjectParser));
        }

        internal static void RegisterMethods()
        {
            RegisterMethod(new Method<ScienceSubject, ScienceExperiment>("Experiment", Experiment));
            RegisterMethod(new Method<ScienceSubject, ExperimentSituations>("Situation", Situation));
            RegisterMethod(new Method<ScienceSubject, CelestialBody>("CelestialBody", CelestialBody));
            RegisterMethod(new Method<ScienceSubject, Biome>("Biome", Biome));

            RegisterGlobalFunction(new Function<List<ScienceSubject>>("AllScienceSubjects", () => Science.GetSubjects(FlightGlobals.Bodies).ToList(), false));
        }

        public SubjectParser()
        {
        }

        private static ScienceExperiment Experiment(ScienceSubject subject)
        {
            if (subject == null || ResearchAndDevelopment.Instance == null)
            {
                return null;
            }

            return ResearchAndDevelopment.GetExperiment(subject.id.Substring(0, subject.id.IndexOf("@")));
        }

        private static ExperimentSituations Situation(ScienceSubject subject)
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

        private static CelestialBody CelestialBody(ScienceSubject subject)
        {
            if (subject == null || ResearchAndDevelopment.Instance == null)
            {
                return null;
            }

            Match m = Regex.Match(subject.id, @"@([A-Z][\w]+?)([A-Z].*)");
            string celestialBody = m.Groups[1].Value;

            return ConfigNodeUtil.ParseCelestialBodyValue(celestialBody);

        }

        private static Biome Biome(ScienceSubject subject)
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

        internal override U ConvertType<U>(ScienceSubject value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.title;
            }
            return base.ConvertType<U>(value);
        }
    }
}
