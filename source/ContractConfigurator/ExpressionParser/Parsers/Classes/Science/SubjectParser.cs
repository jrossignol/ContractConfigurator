using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            RegisterMethod(new Method<ScienceSubject, ScienceExperiment>("Experiment", Science.GetExperiment));
            RegisterMethod(new Method<ScienceSubject, ExperimentSituations>("Situation", Science.GetSituation));
            RegisterMethod(new Method<ScienceSubject, CelestialBody>("CelestialBody", Science.GetCelestialBody));
            RegisterMethod(new Method<ScienceSubject, Biome>("Biome", Science.GetBiome));

            RegisterMethod(new Method<ScienceSubject, float>("CollectedScience", subj => subj == null ? 0.0f : subj.science));
            RegisterMethod(new Method<ScienceSubject, float>("RemainingScience", subj => subj == null ? 0.0f : subj.scienceCap * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier - subj.science));
            RegisterMethod(new Method<ScienceSubject, float>("TotalScience", subj => subj == null ? 0.0f : subj.scienceCap * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier));
            RegisterMethod(new Method<ScienceSubject, float>("NextScienceReportValue", Science.NextScienceReportValue));

            RegisterMethod(new Method<ScienceSubject, string>("SituationString", SituationString));

            RegisterGlobalFunction(new Function<List<ScienceSubject>>("AllScienceSubjects", () => Science.GetSubjects(FlightGlobals.Bodies).ToList(), false));
            RegisterGlobalFunction(new Function<List<CelestialBody>, List<ScienceSubject>>("AllScienceSubjectsByBody", (cbs) => Science.GetSubjects(cbs).ToList(), false));
            RegisterGlobalFunction(new Function<List<ScienceExperiment>, List<ScienceSubject>>("AllScienceSubjectsByExperiment", (exps) => Science.GetSubjects(FlightGlobals.Bodies, x => exps.Contains(x)).ToList(), false));
            RegisterGlobalFunction(new Function<List<Biome>, List<ScienceSubject>>("AllScienceSubjectsByBiome", (biomes) => Science.GetSubjects(biomes.GroupBy(b => b != null ? b.body : null).Select(grp => grp.First() != null ? grp.First().body : null), null, x => biomes.Any(b => b.biome == x)).ToList(), false));

            RegisterGlobalFunction(new Function<List<CelestialBody>, List<ScienceExperiment>, List<ScienceSubject>>("AllScienceSubjectsByBodyExperiment", (cbs, exps) => Science.GetSubjects(cbs, x => exps.Contains(x)).ToList(), false));
            RegisterGlobalFunction(new Function<List<Biome>, List<ScienceExperiment>, List<ScienceSubject>>("AllScienceSubjectsByBiomeExperiment", (biomes, exps) => Science.GetSubjects(biomes.GroupBy(b => b.body).Select(grp => grp.First().body), x => exps.Contains(x), x => biomes.Any(b => b.biome == x)).ToList(), false));

            RegisterGlobalFunction(new Function<List<ScienceSubject>>("DifficultScienceSubjects", () => Science.GetSubjects(FlightGlobals.Bodies, null, null, true).ToList(), false));
            RegisterGlobalFunction(new Function<List<CelestialBody>, List<ScienceSubject>>("DifficultScienceSubjectsByBody", (cbs) => Science.GetSubjects(cbs, null, null, true).ToList(), false));
        }

        public SubjectParser()
        {
        }

        internal override U ConvertType<U>(ScienceSubject value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.title;
            }
            return base.ConvertType<U>(value);
        }

        private static string SituationString(ScienceSubject subject)
        {
            if (subject == null)
            {
                return "";
            }

            ScienceExperiment experiment = Science.GetExperiment(subject);
            return subject.title.Replace(experiment.experimentTitle + " ", "");
        }
    }
}
