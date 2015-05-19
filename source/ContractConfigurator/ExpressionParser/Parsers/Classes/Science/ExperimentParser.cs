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

        internal static void RegisterMethods()
        {
            RegisterGlobalFunction(new Function<List<ScienceExperiment>>("AllExperiments", () => ResearchAndDevelopment.Instance == null ? new List<ScienceExperiment>() :
                ResearchAndDevelopment.GetExperimentIDs().Select<string, ScienceExperiment>(ResearchAndDevelopment.GetExperiment).ToList(), false));
        }

        public ExperimentParser()
        {
        }

        internal override U ConvertType<U>(ScienceExperiment value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.id;
            }
            return base.ConvertType<U>(value);
        }

        internal override ScienceExperiment ParseIdentifier(Token token)
        {
            return ResearchAndDevelopment.GetExperiment(token.sval);
        }
    }
}
