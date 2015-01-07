using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for Orbit ContractParameter.
    /// </summary>
    public class OrbitFactory : ParameterFactory
    {
        protected double minAltitude;
        protected double maxAltitude;
        protected double minApoapsis;
        protected double maxApoapsis;
        protected double minPeriapsis;
        protected double maxPeriapsis;
        protected double minEccentricity;
        protected double maxEccentricity;
        protected double minInclination;
        protected double maxInclination;
        protected double minPeriod;
        protected double maxPeriod;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minAltitude", ref minAltitude, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxAltitude", ref maxAltitude, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minApA", ref minApoapsis, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxApA", ref maxApoapsis, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minPeA", ref minPeriapsis, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxPeA", ref maxPeriapsis, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minEccentricity", ref minEccentricity, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxEccentricity", ref maxEccentricity, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minInclination", ref minInclination, this, 0.0, x => Validation.Between(x, 0.0, 180.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxInclination", ref maxInclination, this, 180.0, x => Validation.Between(x, 0.0, 180.0));

            // Get minPeriod
            string minPeriodStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "minPeriod", ref minPeriodStr, this, (string)null);
            minPeriod = minPeriodStr != null ? DurationUtil.ParseDuration(minPeriodStr) : 0.0;

            // Get maxPeriod
            string maxPeriodStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "maxPeriod", ref maxPeriodStr, this, (string)null);
            if (maxPeriodStr != null)
            {
                maxPeriod = DurationUtil.ParseDuration(maxPeriodStr);
            }
            maxPeriod = maxPeriodStr != null ? DurationUtil.ParseDuration(maxPeriodStr) : double.MaxValue;

            // Validate target body
            valid &= ValidateTargetBody(configNode);

            // Validation minimum and groupings
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "minAltitude", "maxAltitude", "minApA", "maxApA", "minPeA", "maxPeA",
                "minEccentricity", "maxEccentricity", "minInclination", "maxInclination", "minPeriod", "maxPeriod" }, this);
            valid &= ConfigNodeUtil.MutuallyExclusive(configNode, new string[] { "minAltitude", "maxAltitude" },
                new string[] { "minApA", "maxApA", "minPeA", "maxPeA" }, this);


            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitParameter(minAltitude, maxAltitude, minApoapsis, maxApoapsis, minPeriapsis, maxPeriapsis,
                minEccentricity, maxEccentricity, minInclination, maxInclination, minPeriod, maxPeriod, targetBody, title);
        }
    }
}
