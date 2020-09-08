using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

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
        protected double minDeltaVeeActual { get; set; }
        protected double maxDeltaVeeActual { get; set; }
        protected double minDeltaVeeVacuum { get; set; }
        protected double maxDeltaVeeVacuum { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        private static Vessel.Situations[] landedSituations = new Vessel.Situations[] { Vessel.Situations.LANDED, Vessel.Situations.PRELAUNCH, Vessel.Situations.SPLASHED };

        public ReachState()
            : base(null)
        {
        }

        public ReachState(List<CelestialBody> targetBodies, string biome, List<Vessel.Situations> situation, float minAltitude, float maxAltitude,
            float minTerrainAltitude, float maxTerrainAltitude, double minSpeed, double maxSpeed, double minRateOfClimb, double maxRateOfClimb,
            float minAcceleration, float maxAcceleration, double minDeltaVeeActual, double maxDeltaVeeActual, double minDeltaVeeVacuum, double maxDeltaVeeVacuum, string title)
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
            this.minDeltaVeeActual = minDeltaVeeActual;
            this.maxDeltaVeeActual = maxDeltaVeeActual;
            this.minDeltaVeeVacuum = minDeltaVeeVacuum;
            this.maxDeltaVeeVacuum = maxDeltaVeeVacuum;

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
                        output = ParameterDelegate<Vessel>.GetDelegateText(this); ;
                        hideChildren = true;
                    }
                    else
                    {
                        output = Localizer.Format("#cc.param.ReachState.detail", ParameterDelegate<Vessel>.GetDelegateText(this));
                    }
                }
                else
                {
                    Localizer.GetStringByTag("#cc.param.ReachState");
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
                AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.destination", BodyList()),
                    v => targetBodies.Contains(v.mainBody)));
            }

            // Filter for biome
            if (!string.IsNullOrEmpty(biome))
            {
                Biome b = new Biome(targetBodies.First(), biome);
                AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.biome", b), CheckBiome));
            }

            // Filter for situation
            if (situation.Any())
            {
                AddParameter(new ParameterDelegate<Vessel>(Localizer.Format("#cc.param.CollectScience.situation", SituationList()),
                    v => situation.Contains(v.situation)));
            }

            // Filter for altitude
            if (minAltitude != float.MinValue || maxAltitude != float.MaxValue)
            {
                string output;
                if (minAltitude == float.MinValue)
                {
                    output = Localizer.Format("#cc.param.Orbit.below.meters", Localizer.GetStringByTag("#cc.altitude"), maxAltitude.ToString("N0"));
                }
                else if (maxAltitude == float.MaxValue)
                {
                    output = Localizer.Format("#cc.param.Orbit.above.meters", Localizer.GetStringByTag("#cc.altitude"), minAltitude.ToString("N0"));
                }
                else
                {
                    output = Localizer.Format("#cc.param.Orbit.between.meters", Localizer.GetStringByTag("#cc.altitude"), minAltitude.ToString("N0"), maxAltitude.ToString("N0"));
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselAltitude));
            }

            // Filter for terrain altitude
            if (minTerrainAltitude != 0.0f || maxTerrainAltitude != float.MaxValue)
            {
                string output;
                if (minTerrainAltitude == 0.0f)
                {
                    output = Localizer.Format("#cc.param.Orbit.below.meters", Localizer.GetStringByTag("#cc.param.ReachState.altitudeTerrain"), maxTerrainAltitude.ToString("N0"));
                }
                else if (maxTerrainAltitude == float.MaxValue)
                {
                    output = Localizer.Format("#cc.param.Orbit.above.meters", Localizer.GetStringByTag("#cc.param.ReachState.altitudeTerrain"), minTerrainAltitude.ToString("N0"));
                }
                else
                {
                    output = Localizer.Format("#cc.param.Orbit.between.meters", Localizer.GetStringByTag("#cc.param.ReachState.altitudeTerrain"), minTerrainAltitude.ToString("N0"), maxTerrainAltitude.ToString("N0"));
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.heightFromTerrain >= minTerrainAltitude && v.heightFromTerrain <= maxTerrainAltitude));
            }

            // Filter for speed
            if (minSpeed != 0.0 || maxSpeed != double.MaxValue)
            {
                string output;
                if (minSpeed == 0.0)
                {
                    output = Localizer.Format("#cc.param.ReachState.below.speed", Localizer.GetStringByTag("#autoLOC_900381"), maxSpeed.ToString("N0"));
                }
                else if (maxSpeed == double.MaxValue)
                {
                    output = Localizer.Format("#cc.param.ReachState.above.speed", Localizer.GetStringByTag("#autoLOC_900381"), minSpeed.ToString("N0"));
                }
                else
                {
                    output = Localizer.Format("#cc.param.ReachState.between.speed", Localizer.GetStringByTag("#autoLOC_900381"), minSpeed.ToString("N0"), maxSpeed.ToString("N0"));
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselSpeed));
            }

            // Filter for rate of climb
            if (minRateOfClimb != double.MinValue|| maxRateOfClimb != double.MaxValue)
            {
                string output;
                if (minRateOfClimb == double.MinValue)
                {
                    output = Localizer.Format("#cc.param.ReachState.below.speed", Localizer.GetStringByTag("#cc.rateOfClimb"), maxRateOfClimb.ToString("N0"));
                }
                else if (maxRateOfClimb == double.MaxValue)
                {
                    output = Localizer.Format("#cc.param.ReachState.above.speed", Localizer.GetStringByTag("#cc.rateOfClimb"), minRateOfClimb.ToString("N0"));
                }
                else
                {
                    output = Localizer.Format("#cc.param.ReachState.between.speed", Localizer.GetStringByTag("#cc.rateOfClimb"), minRateOfClimb.ToString("N0"), maxRateOfClimb.ToString("N0"));
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselRateOfClimb));
            }

            // Filter for acceleration
            if (minAcceleration != 0.0f || maxAcceleration != float.MaxValue)
            {
                string output;
                if (minAcceleration == 0.0f)
                {
                    output = Localizer.Format("#cc.param.ReachState.below.acceleration", Localizer.GetStringByTag("#cc.acceleration"), maxAcceleration.ToString("F1"));
                }
                else if (maxAcceleration == float.MaxValue)
                {
                    output = Localizer.Format("#cc.param.ReachState.above.acceleration", Localizer.GetStringByTag("#cc.acceleration"), minAcceleration.ToString("F1"));
                }
                else
                {
                    output = Localizer.Format("#cc.param.ReachState.between.acceleration", Localizer.GetStringByTag("#cc.acceleration"), minAcceleration.ToString("F1"), maxAcceleration.ToString("F1"));
                }

                AddParameter(new ParameterDelegate<Vessel>(output, v => v.acceleration.magnitude / 9.81f >= minAcceleration &&
                    v.acceleration.magnitude / 9.81f <= maxAcceleration));
            }

            // Filter for delta-vee (actual)
            if (minDeltaVeeActual != 0.0 || maxDeltaVeeActual != double.MaxValue)
            {
                string output;
                if (minDeltaVeeActual == 0.0)
                {
                    output = Localizer.Format("#cc.param.ReachState.below.speed", Localizer.GetStringByTag("#cc.deltav.actual"), maxDeltaVeeActual.ToString("N0"));
                }
                else if (maxDeltaVeeActual == double.MaxValue)
                {
                    output = Localizer.Format("#cc.param.ReachState.above.speed", Localizer.GetStringByTag("#cc.deltav.actual"), minDeltaVeeActual.ToString("N0"));
                }
                else
                {
                    output = Localizer.Format("#cc.param.ReachState.between.speed", Localizer.GetStringByTag("#cc.deltav.actual"), minDeltaVeeActual.ToString("N0"), maxDeltaVeeActual.ToString("N0"));
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselDeltaVeeActual));
            }

            // Filter for delta-vee (vacuum)
            if (minDeltaVeeVacuum != 0.0 || maxDeltaVeeVacuum != double.MaxValue)
            {
                string output;
                if (minDeltaVeeActual == 0.0)
                {
                    output = Localizer.Format("#cc.param.ReachState.below.speed", Localizer.GetStringByTag("#cc.deltav.vacuum"), maxDeltaVeeVacuum.ToString("N0"));
                }
                else if (maxDeltaVeeVacuum == double.MaxValue)
                {
                    output = Localizer.Format("#cc.param.ReachState.above.speed", Localizer.GetStringByTag("#cc.deltav.vacuum"), minDeltaVeeVacuum.ToString("N0"));
                }
                else
                {
                    output = Localizer.Format("#cc.param.ReachState.between.speed", Localizer.GetStringByTag("#cc.deltav.vacuum"), minDeltaVeeVacuum.ToString("N0"), maxDeltaVeeVacuum.ToString("N0"));
                }

                AddParameter(new ParameterDelegate<Vessel>(output, CheckVesselDeltaVeeVacuum));
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

        private bool CheckVesselDeltaVeeActual(Vessel vessel)
        {
            double deltaVee = vessel.VesselDeltaV.TotalDeltaVActual;
            return deltaVee >= minDeltaVeeActual && deltaVee <= maxDeltaVeeActual;
        }

        private bool CheckVesselDeltaVeeVacuum(Vessel vessel)
        {
            double deltaVee = vessel.VesselDeltaV.TotalDeltaVActual;
            return deltaVee >= minDeltaVeeVacuum && deltaVee <= maxDeltaVeeVacuum;
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
                    if (targetBody != null)
                    {
                        node.AddValue("targetBody", targetBody.name);
                    }
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

            if (minDeltaVeeActual != 0.0)
            {
                node.AddValue("minDeltaVeeActual", minDeltaVeeActual);
            }
            if (maxDeltaVeeActual != double.MaxValue)
            {
                node.AddValue("maxDeltaVeeActual ", maxDeltaVeeActual);
            }

            if (minDeltaVeeVacuum != 0.0)
            {
                node.AddValue("minDeltaVeeVacuum", minDeltaVeeVacuum);
            }
            if (maxDeltaVeeVacuum != double.MaxValue)
            {
                node.AddValue("maxDeltaVeeVacuum ", maxDeltaVeeVacuum);
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
                minDeltaVeeActual = ConfigNodeUtil.ParseValue<double>(node, "minDeltaVeeActual", 0.0);
                maxDeltaVeeActual = ConfigNodeUtil.ParseValue<double>(node, "maxDeltaVeeActual", double.MaxValue);
                minDeltaVeeVacuum = ConfigNodeUtil.ParseValue<double>(node, "minDeltaVeeVacuum", 0.0);
                maxDeltaVeeVacuum = ConfigNodeUtil.ParseValue<double>(node, "maxDeltaVeeVacuum", double.MaxValue);

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
            return LocalizationUtil.LocalizeList<Vessel.Situations>(LocalizationUtil.Conjunction.OR, situation, sit => ReachSituation.GetTitleStringShort(sit));
        }

        public string BodyList()
        {
            return LocalizationUtil.LocalizeList<CelestialBody>(LocalizationUtil.Conjunction.OR, targetBodies, cb => cb.displayName);
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);
            return ParameterDelegate<Vessel>.CheckChildConditions(this, vessel);
        }
    }
}
