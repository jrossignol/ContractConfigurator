using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for verifying the state of our vessel.
    /// </summary>
    public class ReachState : VesselParameter
    {
        protected CelestialBody targetBody { get; set; }
        protected string biome { get; set; }
        protected Vessel.Situations? situation { get; set; }
        protected float minAltitude { get; set; }
        protected float maxAltitude { get; set; }
        protected float minTerrainAltitude { get; set; }
        protected float maxTerrainAltitude { get; set; }
        protected double minSpeed { get; set; }
        protected double maxSpeed { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        private static Vessel.Situations[] landedSituations = new Vessel.Situations[] { Vessel.Situations.LANDED, Vessel.Situations.PRELAUNCH, Vessel.Situations.SPLASHED };

        public ReachState()
            : base(null)
        {
        }

        public ReachState(CelestialBody targetBody, string biome, Vessel.Situations? situation, float minAltitude, float maxAltitude,
            float minTerrainAltitude, float maxTerrainAltitude, double minSpeed, double maxSpeed, string title)
            : base(title)
        {
            this.targetBody = targetBody;
            this.biome = biome;
            this.situation = situation;
            this.minAltitude = minAltitude;
            this.maxAltitude = maxAltitude;
            this.minTerrainAltitude = minTerrainAltitude;
            this.maxTerrainAltitude = maxTerrainAltitude;
            this.minSpeed = minSpeed;
            this.maxSpeed = maxSpeed;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Vessel State";
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

        protected void CreateDelegates()
        {
            // Filter for celestial bodies
            if (targetBody != null)
            {
                AddParameter(new ParameterDelegate<Vessel>("Destination: " + targetBody.theName,
                    v => v.mainBody == targetBody));
            }

            // Filter for biome
            if (!string.IsNullOrEmpty(biome))
            {
                AddParameter(new ParameterDelegate<Vessel>("Biome: " + biome, CheckBiome));
            }

            // Filter for situation
            if (situation != null)
            {
                AddParameter(new ParameterDelegate<Vessel>("Situation: " + ReachSituation.GetTitleStringShort(situation.Value),
                    v => v.situation == situation.Value));
            }

            // Filter for altitude
            if (minAltitude != 0.0f || maxAltitude != float.MaxValue)
            {
                string output = "Altitude: ";
                if (minAltitude == 0.0f)
                {
                    output += "Below " + maxAltitude.ToString("N0") + " m";
                }
                else if (maxAltitude == float.MaxValue)
                {
                    output += "Above " + minAltitude.ToString("N0") + " m";
                }
                else
                {
                    output += "Between " + minAltitude.ToString("N0") + " m and " + maxAltitude.ToString("N0") + " m";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.altitude >= minAltitude && v.altitude <= maxAltitude));
            }

            // Filter for terrain altitude
            if (minTerrainAltitude != 0.0f || maxTerrainAltitude != float.MaxValue)
            {
                string output = "Altitude (terrain): ";
                if (minTerrainAltitude == 0.0f)
                {
                    output += "Below " + maxTerrainAltitude.ToString("N0") + " m";
                }
                else if (maxTerrainAltitude == float.MaxValue)
                {
                    output += "Above " + minTerrainAltitude.ToString("N0") + " m";
                }
                else
                {
                    output += "Between " + minTerrainAltitude.ToString("N0") + " m and " + maxTerrainAltitude.ToString("N0") + " m";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.heightFromTerrain >= minTerrainAltitude && v.heightFromTerrain <= maxTerrainAltitude));
            }

            // Filter for speed
            if (minSpeed != 0.0 || maxSpeed != double.MaxValue)
            {
                string output = "Speed: ";
                if (minSpeed == 0.0)
                {
                    output += "Less than " + maxSpeed.ToString("N0") + " m/s";
                }
                else if (maxSpeed == double.MaxValue)
                {
                    output += "Greater than " + minSpeed.ToString("N0") + " m/s";
                }
                else
                {
                    output += "Between " + minSpeed.ToString("N0") + " m/s and " + maxSpeed.ToString("N0") + " m/s";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselSpeed));
            }
        }

        private bool CheckBiome(Vessel vessel)
        {
            // Fixes problems with special biomes like KSC buildings (total different naming)
            if (landedSituations.Contains(vessel.situation))
            {
                if (Vessel.GetLandedAtString(vessel.landedAt) == biome)
                {
                    return true;
                }
            }

            return ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude) == biome;
        }

        private bool CheckVesselSpeed(Vessel vessel)
        {
            double speed;
            switch (vessel.situation)
            {
                case Vessel.Situations.FLYING:
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                case Vessel.Situations.SPLASHED:
                    speed = vessel.srfSpeed;
                    break;
                default:
                    speed = vessel.obt_speed;
                    break;
            }

            return speed >= minSpeed && speed <= maxSpeed;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            if (targetBody != null)
            {
                node.AddValue("targetBody", targetBody.name);
            }
            node.AddValue("biome", biome);

            if (situation != null)
            {
                node.AddValue("situation", situation);
            }

            node.AddValue("minAltitude", minAltitude);
            if (maxAltitude != float.MaxValue)
            {
                node.AddValue("maxAltitude", maxAltitude);
            }

            node.AddValue("minTerrainAltitude", minTerrainAltitude);
            if (maxTerrainAltitude != float.MaxValue)
            {
                node.AddValue("maxTerrainAltitude", maxTerrainAltitude);
            }

            node.AddValue("minSpeed", minSpeed);
            if (maxSpeed != Double.MaxValue)
            {
                node.AddValue("maxSpeed", maxSpeed);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                base.OnParameterLoad(node);
                targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody", (CelestialBody)null);
                biome = ConfigNodeUtil.ParseValue<string>(node, "biome");
                situation = ConfigNodeUtil.ParseValue<Vessel.Situations?>(node, "situation", (Vessel.Situations?)null);
                minAltitude = ConfigNodeUtil.ParseValue<float>(node, "minAltitude");
                maxAltitude = ConfigNodeUtil.ParseValue<float>(node, "maxAltitude", float.MaxValue);
                minTerrainAltitude = ConfigNodeUtil.ParseValue<float>(node, "minTerrainAltitude", 0.0f);
                maxTerrainAltitude = ConfigNodeUtil.ParseValue<float>(node, "maxTerrainAltitude", float.MaxValue);
                minSpeed = ConfigNodeUtil.ParseValue<double>(node, "minSpeed");
                maxSpeed = ConfigNodeUtil.ParseValue<double>(node, "maxSpeed", Double.MaxValue);

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
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);

        }
    }
}
