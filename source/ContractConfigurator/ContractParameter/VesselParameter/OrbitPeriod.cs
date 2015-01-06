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
     * Parameter for checking vessels periapsis
     */
    public class OrbitPeriod : VesselParameter
    {
        protected string title { get; set; }
        protected double minPeriod { get; set; }
        protected double maxPeriod { get; set; }
        protected CelestialBody targetBody { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public OrbitPeriod()
            : this(0.0, 0.0, null)
        {
        }

        public OrbitPeriod(double minPeriod, double maxPeriod, CelestialBody targetBody, string title = null)
            : base()
        {
            this.minPeriod = minPeriod;
            this.maxPeriod = maxPeriod;
            this.targetBody = targetBody;

            if (title == null)
            {
                this.title = "Period: ";
                this.title += "between " + DurationUtil.StringValue(minPeriod) + " and " + DurationUtil.StringValue(maxPeriod);
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
            node.AddValue("minPeriod", minPeriod);
            node.AddValue("maxPeriod", maxPeriod); 
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minPeriod = Convert.ToDouble(node.GetValue("minPeriod"));
            maxPeriod = Convert.ToDouble(node.GetValue("maxPeriod"));
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
                double period = vessel.orbit.period;
                return period >= minPeriod && period <= maxPeriod;
            }

            return false;
        }
    }
}
