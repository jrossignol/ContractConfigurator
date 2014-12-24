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
    public class OrbitEccentricity : VesselParameter
    {
        protected string title { get; set; }
        protected double minEccentricity { get; set; }
        protected double maxEccentricity { get; set; }
        protected CelestialBody targetBody { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public OrbitEccentricity()
            : this(0.0, 1, null)
        {
        }

        public OrbitEccentricity(double minEccentricity, double maxEccentricity, CelestialBody targetBody, string title = null)
            : base()
        {
            this.minEccentricity = minEccentricity;
            this.maxEccentricity = maxEccentricity;
            this.targetBody = targetBody;

            if (title == null)
            {
                this.title = "Reach and hold an eccentricity between " + minEccentricity + " and " + maxEccentricity;
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
            node.AddValue("minEccentricity", minEccentricity);
            node.AddValue("maxEccentricity", maxEccentricity);
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minEccentricity = Convert.ToDouble(node.GetValue("minEccentricity"));
            maxEccentricity = Convert.ToDouble(node.GetValue("maxEccentricity"));
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
                double eccentricity = vessel.orbit.eccentricity;
                return eccentricity >= minEccentricity && eccentricity <= maxEccentricity;
            }

            return false;
        }
    }
}
