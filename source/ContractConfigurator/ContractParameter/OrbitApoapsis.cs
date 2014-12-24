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
     * Parameter for checking vessels inclination
     */
    public class OrbitApoapsis : VesselParameter
    {
        protected string title { get; set; }
        protected double minApoapsis { get; set; }
        protected double maxApoapsis { get; set; }
        protected CelestialBody targetBody { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public OrbitApoapsis()
            : this(0.0, 180.0, null)
        {
        }

        public OrbitApoapsis(double minApoapsis, double maxApoapsis, CelestialBody targetBody, string title = null)
            : base()
        {
            this.minApoapsis = minApoapsis;
            this.maxApoapsis = maxApoapsis;
            this.targetBody = targetBody;

            if (title == null)
            {
                this.title = "Reach and hold an apoapsis between " + minApoapsis + "m and " + maxApoapsis + "m";
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
            node.AddValue("minApoapsis", minApoapsis);
            node.AddValue("maxApoapsis", maxApoapsis);
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minApoapsis = Convert.ToDouble(node.GetValue("minApoapsis"));
            maxApoapsis = Convert.ToDouble(node.GetValue("maxApoapsis"));
            targetBody = ConfigNodeUtil.ParseCelestialBody(node, "targetBody");
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
            if (vessel.mainBody == targetBody && vessel.situation == Vessel.Situations.ORBITING)
            {
                double apoapsis = vessel.orbit.ApA;
                return apoapsis >= minApoapsis && apoapsis <= maxApoapsis;
            }

            return false;
        }
    }
}
