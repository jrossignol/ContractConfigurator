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
    /// Expression parser subclass for Experiment.
    /// </summary>
    public class ExperimentSituationParser : EnumExpressionParser<ExperimentSituations>, IExpressionParserRegistrer
    {
        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(ExperimentSituations), typeof(ExperimentSituationParser));
        }

        public ExperimentSituationParser()
        {
        }

        internal override U ConvertType<U>(ExperimentSituations value)
        {
            if (typeof(U) == typeof(string))
            {
                return (U)(object)value.Print();
            }
            return base.ConvertType<U>(value);
        }
    }
}
