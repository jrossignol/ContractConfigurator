using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking various orbital parameters.
    /// </summary>
    public class OrbitParameter : VesselParameter
    {
        protected Vessel.Situations situation { get; set; }
        protected double minAltitude { get; set; }
        protected double maxAltitude { get; set; }
        protected double minApoapsis { get; set; }
        protected double maxApoapsis { get; set; }
        protected double minPeriapsis { get; set; }
        protected double maxPeriapsis { get; set; }
        protected double minEccentricity { get; set; }
        protected double maxEccentricity { get; set; }
        protected double minInclination { get; set; }
        protected double maxInclination { get; set; }
        protected double minArgumentOfPeriapsis { get; set; }
        protected double maxArgumentOfPeriapsis { get; set; }
        protected double minPeriod { get; set; }
        protected double maxPeriod { get; set; }
        protected Orbit orbit;
        protected double deviationWindow;
        protected bool displayNotes;

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        private SpecificOrbitParameter sop;

        public OrbitParameter()
            : base(null)
        {
        }

        public OrbitParameter(Vessel.Situations situation, double minAltitude, double maxAltitude, double minApoapsis, double maxApoapsis, double minPeriapsis, double maxPeriapsis,
            double minEccentricity, double maxEccentricity, double minInclination, double maxInclination, double minArgumentOfPeriapsis, double maxArgumentOfPeriapsis, double minPeriod, double maxPeriod, 
            CelestialBody targetBody, string title = null)
            : base(title)
        {
            this.situation = situation;
            this.targetBody = targetBody;
            this.minAltitude = minAltitude;
            this.maxAltitude = maxAltitude;
            this.minApoapsis = minApoapsis;
            this.maxApoapsis = maxApoapsis;
            this.minPeriapsis = minPeriapsis;
            this.maxPeriapsis = maxPeriapsis;
            this.minEccentricity = minEccentricity;
            this.maxEccentricity = maxEccentricity;
            this.minInclination = minInclination;
            this.maxInclination = maxInclination;
            this.minArgumentOfPeriapsis = minArgumentOfPeriapsis;
            this.maxArgumentOfPeriapsis = maxArgumentOfPeriapsis;
            this.minPeriod = minPeriod;
            this.maxPeriod = maxPeriod;
            this.displayNotes = false;
            orbit = null;

            CreateDelegates();
        }

        public OrbitParameter(Orbit orbit, double deviationWindow, bool displayNotes)
        {
            targetBody = orbit.referenceBody;
            minAltitude = 0.0;
            maxAltitude = double.MaxValue;
            minApoapsis = 0.0;
            maxApoapsis = double.MaxValue;
            minPeriapsis = 0.0;
            maxPeriapsis = double.MaxValue;
            minEccentricity = 0.0;
            maxEccentricity = double.MaxValue;
            minInclination = 0.0;
            maxInclination = 180;
            minPeriod = 0.0;
            maxPeriod = double.MaxValue;
            this.orbit = orbit;
            this.deviationWindow = deviationWindow;
            this.displayNotes = displayNotes;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Orbit";
                if (state == ParameterState.Complete)
                {
                    output += ": " + ParameterDelegate<Vessel>.GetDelegateText(this);
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected override string GetNotes()
        {
            if (displayNotes)
            {
                if (sop == null)
                {
                    sop = new SpecificOrbitParameter(OrbitType.POLAR, orbit.inclination, orbit.eccentricity, orbit.semiMajorAxis, orbit.LAN, orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, orbit.epoch, targetBody, deviationWindow);
                }
                return sop.Notes;
            }
            else
            {
                return notes;
            }
        }

        protected void CreateDelegates()
        {
            // Filter for celestial bodies
            if (targetBody != null)
            {
                AddParameter(new ParameterDelegate<Vessel>("Destination: " + targetBody.theName,
                    v => v.mainBody == targetBody, true));
            }

            // Filter for situation
            if (orbit == null)
            {
            AddParameter(new ParameterDelegate<Vessel>("Situation: " + ReachSituation.GetTitleStringShort(situation),
                v => v.situation == situation, true));
            }

            // Filter for altitude
            if (minAltitude != 0.0 || maxAltitude != double.MaxValue)
            {
                string output = "Altitude: ";
                if (minAltitude == 0.0)
                {
                    output += "Below " + maxAltitude.ToString("N0") + " m";
                }
                else if (maxAltitude == double.MaxValue)
                {
                    output += "Above " + minAltitude.ToString("N0") + " m";
                }
                else
                {
                    output += "Between " + minAltitude.ToString("N0") + " m and " + maxAltitude.ToString("N0") + " m";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.orbit.PeA >= minAltitude && v.orbit.ApA <= maxAltitude));
            }

            // Filter for apoapsis
            if (minApoapsis != 0.0 || maxApoapsis != double.MaxValue)
            {
                string output = "Apoapsis: ";
                if (minApoapsis == 0.0)
                {
                    output += "Below " + maxApoapsis.ToString("N0") + " m";
                }
                else if (maxApoapsis == double.MaxValue)
                {
                    output += "Above " + minApoapsis.ToString("N0") + " m";
                }
                else
                {
                    output += "Between " + minApoapsis.ToString("N0") + " m and " + maxApoapsis.ToString("N0") + " m";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.orbit.ApA >= minApoapsis && v.orbit.ApA <= maxApoapsis));
            }

            // Filter for periapsis
            if (minPeriapsis != 0.0 || maxPeriapsis != double.MaxValue)
            {
                string output = "Periapsis: ";
                if (minPeriapsis == 0.0)
                {
                    output += "Below " + maxPeriapsis.ToString("N0") + " m";
                }
                else if (maxPeriapsis == double.MaxValue)
                {
                    output += "Above " + minPeriapsis.ToString("N0") + " m";
                }
                else
                {
                    output += "Between " + minPeriapsis.ToString("N0") + " m and " + maxPeriapsis.ToString("N0") + " m";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.orbit.PeA >= minPeriapsis && v.orbit.PeA <= maxPeriapsis));
            }

            // Filter for eccentricity
            if (minEccentricity != 0.0 || maxEccentricity != double.MaxValue)
            {
                string output = "Eccentricity: ";
                if (minEccentricity == 0.0)
                {
                    output += "Below " + maxEccentricity.ToString("F4");
                }
                else if (maxEccentricity == double.MaxValue)
                {
                    output += "Above " + minEccentricity.ToString("F4");
                }
                else
                {
                    output += "Between " + minEccentricity.ToString("F4") + " and " + maxEccentricity.ToString("F4");
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.orbit.eccentricity >= minEccentricity && v.orbit.eccentricity <= maxEccentricity));
            }

            // Filter for inclination
            if (minInclination != 0.0 || maxInclination != 180.0)
            {
                string output = "Inclination: ";
                if (minInclination == 0.0)
                {
                    output += "Below " + maxInclination.ToString("F1") + "°";
                }
                else if (maxInclination == 180.0)
                {
                    output += "Above " + minInclination.ToString("F1") + "°";
                }
                else
                {
                    output += "Between " + minInclination.ToString("F1") + "° and " + maxInclination.ToString("F1") + "°";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckInclination));
            }

            // Filter for argument of periapsis
            if (minArgumentOfPeriapsis != 0.0 || maxArgumentOfPeriapsis != 360.0)
            {
                string output = "Argument of Periapsis: Between " +
                    minArgumentOfPeriapsis.ToString("F1") + "° and " + maxArgumentOfPeriapsis.ToString("F1") + "°";

                AddParameter(new ParameterDelegate<Vessel>(output, CheckAoP));
            }

            // Filter for orbital period
            if (minPeriod != 0.0 || maxPeriod != double.MaxValue)
            {
                string output = "Period: ";
                if (minPeriod == 0.0)
                {
                    output += "Below " + DurationUtil.StringValue(maxPeriod, false);
                }
                else if (maxPeriod == double.MaxValue)
                {
                    output += "Above " + DurationUtil.StringValue(minPeriod, false);
                }
                else
                {
                    output += "Between " + DurationUtil.StringValue(minPeriod, false) + " and " + DurationUtil.StringValue(maxPeriod, false);
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.orbit.period >= minPeriod && v.orbit.period <= maxPeriod));
            }

            // Filter for specific orbit
            if (orbit != null)
            {
                string output = "Reach the specified orbit";
                AddParameter(new ParameterDelegate<Vessel>(output, v => VesselUtilities.VesselAtOrbit(orbit, deviationWindow, v)));
            }
        }

        private bool CheckInclination(Vessel vessel)
        {
            double inclination = vessel.orbit.inclination;

            // Inclination can momentarily be in the [0.0, 360] range before KSP adjusts it
            if (inclination > 180.0)
            {
                inclination = 360 - inclination;
            }

            return inclination >= minInclination && inclination <= maxInclination;
        }

        private bool CheckAoP(Vessel vessel)
        {
            double aop = vessel.orbit.argumentOfPeriapsis;

            // Normalise
            while (aop < 0.0)
            {
                aop += 360.0;
            }
            while (aop > 360.0)
            {
                aop -= 360.0;
            }

            return aop >= minArgumentOfPeriapsis && aop <= maxArgumentOfPeriapsis;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("situation", situation);
            if (targetBody != null)
            {
                node.AddValue("targetBody", targetBody.name);
            }
            node.AddValue("minAltitude", minAltitude);
            if (maxAltitude != double.MaxValue)
            {
                node.AddValue("maxAltitude", maxAltitude);
            }
            node.AddValue("minApoapsis", minApoapsis);
            if (maxApoapsis != double.MaxValue)
            {
                node.AddValue("maxApoapsis", maxApoapsis);
            }
            node.AddValue("minPeriapsis", minPeriapsis);
            if (maxPeriapsis != double.MaxValue)
            {
                node.AddValue("maxPeriapsis", maxPeriapsis);
            }
            node.AddValue("minEccentricity", minEccentricity);
            if (maxEccentricity != double.MaxValue)
            {
                node.AddValue("maxEccentricity", maxEccentricity);
            }
            node.AddValue("minInclination", minInclination);
            if (maxInclination != double.MaxValue)
            {
                node.AddValue("maxInclination", maxInclination);
            }
            node.AddValue("minArgumentOfPeriapsis", minArgumentOfPeriapsis);
            node.AddValue("maxArgumentOfPeriapsis", maxArgumentOfPeriapsis);
            node.AddValue("minPeriod", minPeriod);
            if (maxPeriod != double.MaxValue)
            {
                node.AddValue("maxPeriod", maxPeriod);
            }
            node.AddValue("displayNotes", displayNotes);
            if (orbit != null)
            {
                ConfigNode orbitNode = new ConfigNode("ORBIT");
                new OrbitSnapshot(orbit).Save(orbitNode);
                node.AddNode(orbitNode);
                node.AddValue("deviationWindow", deviationWindow);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                base.OnParameterLoad(node);
                situation = ConfigNodeUtil.ParseValue<Vessel.Situations>(node, "situation", Vessel.Situations.ORBITING);
                minAltitude = ConfigNodeUtil.ParseValue<double>(node, "minAltitude");
                maxAltitude = ConfigNodeUtil.ParseValue<double>(node, "maxAltitude", double.MaxValue);
                minApoapsis = ConfigNodeUtil.ParseValue<double>(node, "minApoapsis");
                maxApoapsis = ConfigNodeUtil.ParseValue<double>(node, "maxApoapsis", double.MaxValue);
                minPeriapsis = ConfigNodeUtil.ParseValue<double>(node, "minPeriapsis");
                maxPeriapsis = ConfigNodeUtil.ParseValue<double>(node, "maxPeriapsis", double.MaxValue);
                minEccentricity = ConfigNodeUtil.ParseValue<double>(node, "minEccentricity");
                maxEccentricity = ConfigNodeUtil.ParseValue<double>(node, "maxEccentricity", double.MaxValue);
                minInclination = ConfigNodeUtil.ParseValue<double>(node, "minInclination");
                maxInclination = ConfigNodeUtil.ParseValue<double>(node, "maxInclination", double.MaxValue);
                minArgumentOfPeriapsis = ConfigNodeUtil.ParseValue<double>(node, "minArgumentOfPeriapsis", 0.0);
                maxArgumentOfPeriapsis = ConfigNodeUtil.ParseValue<double>(node, "maxArgumentOfPeriapsis", 360.0);
                minPeriod = ConfigNodeUtil.ParseValue<double>(node, "minPeriod");
                maxPeriod = ConfigNodeUtil.ParseValue<double>(node, "maxPeriod", double.MaxValue);
                targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
                displayNotes = ConfigNodeUtil.ParseValue<bool?>(node, "displayNotes", (bool?)false).Value;

                if (node.HasNode("ORBIT"))
                {
                    orbit = new OrbitSnapshot(node.GetNode("ORBIT")).Load();
                    deviationWindow = ConfigNodeUtil.ParseValue<double>(node, "deviationWindow");
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<Vessel>.OnDelegateContainerLoad(node);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                CheckVessel(FlightGlobals.ActiveVessel);
            }
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check.</param>
        /// <returns>Whether the vessel meets the conditions.</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
        }
    }
}
