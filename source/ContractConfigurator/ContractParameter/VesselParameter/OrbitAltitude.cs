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
    /*
     * Parameter for checking vessels orbit
     */
    public class OrbitAltitude : VesselParameter
    {
        protected string title { get; set; }
        protected double minAltitude { get; set; }
        protected double maxAltitude { get; set; }
        protected CelestialBody targetBody { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public OrbitAltitude()
            : this(0.0, 0.0, null)
        {
        }

        public OrbitAltitude(double minAltitude, double maxAltitude, CelestialBody targetBody, string title = null)
            : base()
        {
            this.minAltitude = minAltitude;
            this.maxAltitude = maxAltitude;
            this.targetBody = targetBody;

            if (title == null)
            {
                this.title = "Orbit: ";
                if (minAltitude == 0.0)
                {
                    this.title += "below " + maxAltitude.ToString("N0") + "m";
                }
                else if (maxAltitude == double.MaxValue)
                {
                    this.title += "above " + minAltitude.ToString("N0") + "m";
                }
                else
                {
                    this.title += "between " + minAltitude.ToString("N0") + "m and " + maxAltitude.ToString("N0") + "m";
                }
            }
            else
            {
                this.title = title;
            }
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("minAltitude", minAltitude);
            if (maxAltitude != double.MaxValue)
            {
                node.AddValue("maxAltitude", maxAltitude);
            }
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minAltitude = Convert.ToDouble(node.GetValue("minAltitude"));
            maxAltitude = node.HasValue("maxAltitude") ? Convert.ToDouble(node.GetValue("maxAltitude")) : double.MaxValue;
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
        }

        protected override void OnRegister()
        {
            base.OnRegister();
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
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

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            if (vessel.mainBody == targetBody && vessel.situation == Vessel.Situations.ORBITING)
            {
                return vessel.orbit.PeA >= minAltitude && vessel.orbit.ApA <= maxAltitude;
            }

            return false;
        }
    }
}
