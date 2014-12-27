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
    public class OrbitPeriapsis : VesselParameter
    {
        protected string title { get; set; }
        protected double minPeriapsis { get; set; }
        protected double maxPeriapsis { get; set; }
        protected CelestialBody targetBody { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public OrbitPeriapsis()
            : this(0.0, 0.0, null)
        {
        }

        public OrbitPeriapsis(double minPeriapsis, double maxPeriapsis, CelestialBody targetBody, string title = null)
            : base()
        {
            this.minPeriapsis = minPeriapsis;
            this.maxPeriapsis = maxPeriapsis;
            this.targetBody = targetBody;

            if (title == null)
            {
                this.title = "Periapsis: ";
                if (minPeriapsis == 0.0)
                {
                    this.title += "below " + maxPeriapsis.ToString("N0") + "m";
                }
                else if (maxPeriapsis == double.MaxValue)
                {
                    this.title += "above " + minPeriapsis.ToString("N0") + "m";
                }
                else
                {
                    this.title += "between " + minPeriapsis.ToString("N0") + "m and " + maxPeriapsis.ToString("N0") + "m";
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
            node.AddValue("minPeriapsis", minPeriapsis);
            if (maxPeriapsis != double.MaxValue)
            {
                node.AddValue("maxPeriapsis", maxPeriapsis);
            }
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minPeriapsis = Convert.ToDouble(node.GetValue("minPeriapsis"));
            maxPeriapsis = node.HasValue("maxPeriapsis") ? Convert.ToDouble(node.GetValue("maxPeriapsis")) : double.MaxValue;
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
            if (vessel.mainBody == targetBody && vessel.situation != Vessel.Situations.LANDED)
            {
                double periapsis = vessel.orbit.PeA;
                return periapsis >= minPeriapsis && periapsis <= maxPeriapsis;
            }

            return false;
        }
    }
}
