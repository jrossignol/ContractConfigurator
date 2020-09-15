using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ContractConfigurator.ExpressionParser
{
    /// <summary>
    /// Expression parser subclass for Experiment.
    /// </summary>
    public class ExperimentParser : ClassExpressionParser<ScienceExperiment>, IExpressionParserRegistrer
    {
        static ExperimentParser()
        {
            RegisterMethods();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(ScienceExperiment), typeof(ExperimentParser));
        }

        public static void RegisterMethods()
        {
            RegisterMethod(new Method<ScienceExperiment, string>("Name", e => e == null ? "" : e.experimentTitle));
            RegisterMethod(new Method<ScienceExperiment, string>("ID", e => e == null ? "" : e.id));

            RegisterGlobalFunction(new Function<List<ScienceExperiment>>("AllExperiments", () => ResearchAndDevelopment.Instance == null ? new List<ScienceExperiment>() :
                ResearchAndDevelopment.GetExperimentIDs().Select<string, ScienceExperiment>(ResearchAndDevelopment.GetExperiment).ToList(), false));
            RegisterGlobalFunction(new Function<CelestialBody, List<ScienceExperiment>>("AvailableExperiments", (cb) => Util.Science.AvailableExperiments(cb).ToList(), false));
        }

        public ExperimentParser()
        {
        }

        public override U ConvertType<U>(ScienceExperiment value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.experimentTitle;
            }
            return base.ConvertType<U>(value);
        }

        public override ScienceExperiment ParseIdentifier(Token token)
        {
            return ResearchAndDevelopment.GetExperiment(token.sval);
        }
    }
}
