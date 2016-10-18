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
        public List<CelestialBody> targetBodies { get; set; }
        protected string biome { get; set; }
        protected List<Vessel.Situations> situation { get; set; }
        protected float minAltitude { get; set; }
        protected float maxAltitude { get; set; }
        protected float minTerrainAltitude { get; set; }
        protected float maxTerrainAltitude { get; set; }
        protected double minSpeed { get; set; }
        protected double maxSpeed { get; set; }
        protected double minRateOfClimb { get; set; }
        protected double maxRateOfClimb { get; set; }
        protected float minAcceleration { get; set; }
        protected float maxAcceleration { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        private static Vessel.Situations[] landedSituations = new Vessel.Situations[] { Vessel.Situations.LANDED, Vessel.Situations.PRELAUNCH, Vessel.Situations.SPLASHED };

        public ReachState()
            : base(null)
        {
        }

        public ReachState(List<CelestialBody> targetBodies, string biome, List<Vessel.Situations> situation, float minAltitude, float maxAltitude,
            float minTerrainAltitude, float maxTerrainAltitude, double minSpeed, double maxSpeed, double minRateOfClimb, double maxRateOfClimb,
            float minAcceleration, float maxAcceleration, string title)
            : base(title)
        {
            this.targetBodies = targetBodies;
            this.biome = biome;
            this.situation = situation;
            this.minAltitude = minAltitude;
            this.maxAltitude = maxAltitude;
            this.minTerrainAltitude = minTerrainAltitude;
            this.maxTerrainAltitude = maxTerrainAltitude;
            this.minSpeed = minSpeed;
            this.maxSpeed = maxSpeed;
            this.minRateOfClimb = minRateOfClimb;
            this.maxRateOfClimb = maxRateOfClimb;
            this.minAcceleration = minAcceleration;
            this.maxAcceleration = maxAcceleration;

            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                output = "Vessel State";
                if (state == ParameterState.Complete || ParameterCount == 1)
                {
                    if (ParameterCount == 1)
                    {
                        output = "";
                        hideChildren = true;
                    }
                    else
                    {
                        output += ": ";
                    }

                    output += ParameterDelegate<Vessel>.GetDelegateText(this);
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
            if (targetBodies != null && targetBodies.Count() != FlightGlobals.Bodies.Count)
            {
                AddParameter(new ParameterDelegate<Vessel>("Destination: " + BodyList(),
                    v => targetBodies.Contains(v.mainBody)));
            }

            // Filter for biome
            if (!string.IsNullOrEmpty(biome))
            {
                AddParameter(new ParameterDelegate<Vessel>("Biome: " + biome, CheckBiome));
            }

            // Filter for situation
            if (situation.Any())
            {
                AddParameter(new ParameterDelegate<Vessel>("Situation: " + SituationList(),
                    v => situation.Contains(v.situation)));
            }

            // Filter for altitude
            if (minAltitude != float.MinValue || maxAltitude != float.MaxValue)
            {
                string output = "Altitude: ";
                if (minAltitude == float.MinValue)
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

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselAltitude));
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

            // Filter for rate of climb
            if (minRateOfClimb != double.MinValue|| maxRateOfClimb != double.MaxValue)
            {
                string output = "Rate of Climb: ";
                if (minRateOfClimb == double.MinValue)
                {
                    output += "Less than " + maxRateOfClimb.ToString("N0") + " m/s";
                }
                else if (maxRateOfClimb == double.MaxValue)
                {
                    output += "Greater than " + minRateOfClimb.ToString("N0") + " m/s";
                }
                else
                {
                    output += "Between " + minRateOfClimb.ToString("N0") + " m/s and " + maxRateOfClimb.ToString("N0") + " m/s";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselRateOfClimb));
            }

            // Filter for acceleration
            if (minAcceleration != 0.0f || maxAcceleration != float.MaxValue)
            {
                string output = "Acceleration: ";
                if (minAcceleration == 0.0f)
                {
                    output += "Less than " + maxAcceleration.ToString("F1") + " gee" + (maxAcceleration == 1.0f ? "" : "s");
                }
                else if (maxAcceleration == float.MaxValue)
                {
                    output += "Greater than " + minAcceleration.ToString("F1") + " gee" + (maxAcceleration == 1.0f ? "" : "s");
                }
                else
                {
                    output += "Between " + minAcceleration.ToString("F1") + " and " + maxAcceleration.ToString("F1") + " gees";
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.acceleration.magnitude / 9.81f >= minAcceleration &&
                    v.acceleration.magnitude / 9.81f <= maxAcceleration));
            }
        }

        private bool CheckBiome(Vessel vessel)
        {
            // Fixes problems with special biomes like KSC buildings (total different naming)
            if (landedSituations.Contains(vessel.situation))
            {
                if (vessel.landedAt != null && Vessel.GetLandedAtString(vessel.landedAt) == biome)
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

            // Round it to avoid issues when checking for a zero speed
            speed = Math.Round(speed, maxSpeed > 0.5 ? 1 : 0);

            return speed >= minSpeed && speed <= maxSpeed;
        }

        private bool CheckVesselRateOfClimb(Vessel vessel)
        {
            Vector3d nrm = FlightGlobals.currentMainBody.GetSurfaceNVector(vessel.latitude, vessel.longitude);
            double speed = Vector3d.Dot(vessel.srf_velocity, nrm);

            return speed >= minRateOfClimb && speed <= maxRateOfClimb;
        }

        private bool CheckVesselAltitude(Vessel vessel)
        {
            return vessel.altitude >= minAltitude && vessel.altitude <= maxAltitude;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            if (targetBodies != null)
            {
                foreach (CelestialBody targetBody in targetBodies)
                {
                    node.AddValue("targetBody", targetBody.name);
                }
            }
            if (!string.IsNullOrEmpty(biome))
            {
                node.AddValue("biome", biome);
            }

            
            foreach (Vessel.Situations sit in situation)
            {
                node.AddValue("situation", sit);
            }

            if (minAltitude != float.MinValue)
            {
                node.AddValue("minAltitude", minAltitude);
            }
            if (maxAltitude != float.MaxValue)
            {
                node.AddValue("maxAltitude", maxAltitude);
            }

            if (minTerrainAltitude != 0.0f)
            {
                node.AddValue("minTerrainAltitude", minTerrainAltitude);
            }
            if (maxTerrainAltitude != float.MaxValue)
            {
                node.AddValue("maxTerrainAltitude", maxTerrainAltitude);
            }

            if (minSpeed != 0.0)
            {
                node.AddValue("minSpeed", minSpeed);
            }
            if (maxSpeed != double.MaxValue)
            {
                node.AddValue("maxSpeed", maxSpeed);
            }

            if (minRateOfClimb != double.MinValue)
            {
                node.AddValue("minRateOfClimb", minRateOfClimb);
            }
            if (maxRateOfClimb != double.MaxValue)
            {
                node.AddValue("maxRateOfClimb", maxRateOfClimb);
            }

            if (minAcceleration != 0.0f)
            {
                node.AddValue("minAcceleration", minAcceleration);
            }
            if (maxAcceleration != float.MaxValue)
            {
                node.AddValue("maxAcceleration", maxAcceleration);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                base.OnParameterLoad(node);
                targetBodies = ConfigNodeUtil.ParseValue<List<CelestialBody>>(node, "targetBody", null);
                biome = ConfigNodeUtil.ParseValue<string>(node, "biome", "");
                situation = ConfigNodeUtil.ParseValue<List<Vessel.Situations>>(node, "situation", new List<Vessel.Situations>());
                minAltitude = ConfigNodeUtil.ParseValue<float>(node, "minAltitude", float.MinValue);
                maxAltitude = ConfigNodeUtil.ParseValue<float>(node, "maxAltitude", float.MaxValue);
                minTerrainAltitude = ConfigNodeUtil.ParseValue<float>(node, "minTerrainAltitude", 0.0f);
                maxTerrainAltitude = ConfigNodeUtil.ParseValue<float>(node, "maxTerrainAltitude", float.MaxValue);
                minSpeed = ConfigNodeUtil.ParseValue<double>(node, "minSpeed", 0.0);
                maxSpeed = ConfigNodeUtil.ParseValue<double>(node, "maxSpeed", double.MaxValue);
                minRateOfClimb = ConfigNodeUtil.ParseValue<double>(node, "minRateOfClimb", double.MinValue);
                maxRateOfClimb = ConfigNodeUtil.ParseValue<double>(node, "maxRateOfClimb", double.MaxValue);
                minAcceleration = ConfigNodeUtil.ParseValue<float>(node, "minAcceleration", 0.0f);
                maxAcceleration = ConfigNodeUtil.ParseValue<float>(node, "maxAcceleration", float.MaxValue);

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

        public string SituationList()
        {
            Vessel.Situations first = situation.First();
            Vessel.Situations last = situation.Last();
            string result = ReachSituation.GetTitleStringShort(first);
            foreach (Vessel.Situations sit in situation.Where(s => s != first && s != last))
            {
                result += ", " + ReachSituation.GetTitleStringShort(sit);
            }
            if (last != first)
            {
                result += " or " + ReachSituation.GetTitleStringShort(last);
            }
            return result;
        }

        public string BodyList()
        {
            CelestialBody first = targetBodies.First();
            CelestialBody last = targetBodies.Last();
            string result = first.theName;
            foreach (CelestialBody body in targetBodies.Where(b => b != first && b != last))
            {
                result += ", " + body.theName;
            }
            if (last != first)
            {
                result += " or " + last.theName;
            }
            return result;
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
