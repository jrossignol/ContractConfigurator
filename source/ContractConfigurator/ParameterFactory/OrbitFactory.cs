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
        protected Vessel.Situations situation;
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

            valid &= ConfigNodeUtil.ParseValue<Vessel.Situations>(configNode, "situation", x => situation = x, this, Vessel.Situations.ORBITING, ValidateSituations);
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minAltitude", x => minAltitude = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxAltitude", x => maxAltitude = x, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minApA", x => minApoapsis = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxApA", x => maxApoapsis = x, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minPeA", x => minPeriapsis = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxPeA", x => maxPeriapsis = x, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minEccentricity", x => minEccentricity = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxEccentricity", x => maxEccentricity = x, this, double.MaxValue, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minInclination", x => minInclination = x, this, 0.0, x => Validation.Between(x, 0.0, 180.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxInclination", x => maxInclination = x, this, 180.0, x => Validation.Between(x, 0.0, 180.0));

            // Get minPeriod
            string minPeriodStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "minPeriod", x => minPeriodStr = x, this, (string)null);
            minPeriod = minPeriodStr != null ? DurationUtil.ParseDuration(minPeriodStr) : 0.0;

            // Get maxPeriod
            string maxPeriodStr = null;
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "maxPeriod", x => maxPeriodStr = x, this, (string)null);
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

        private bool ValidateSituations(Vessel.Situations situation)
        {
            if (situation != Vessel.Situations.ESCAPING &&
                situation != Vessel.Situations.ORBITING &&
                situation != Vessel.Situations.SUB_ORBITAL)
            {
                LoggingUtil.LogError(this, "Invalid situation for Orbit parameter: " + situation + ".  For non-orbital situations, use ReachState instead.");
                return false;
            }
            return true;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new OrbitParameter(situation, minAltitude, maxAltitude, minApoapsis, maxApoapsis, minPeriapsis, maxPeriapsis,
                minEccentricity, maxEccentricity, minInclination, maxInclination, minPeriod, maxPeriod, targetBody, title);
        }
    }
}
