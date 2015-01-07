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
    [Obsolete("Obsolete, use OrbitParameter")]
    public class OrbitInclination : VesselParameter
    {
        protected string title { get; set; }
        protected double minInclination { get; set; }
        protected double maxInclination { get; set; }
        protected CelestialBody targetBody { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public OrbitInclination()
            : this(0.0, 180.0, null)
        {
        }

        public OrbitInclination(double minInclination, double maxInclination, CelestialBody targetBody, string title = null)
            : base()
        {
            this.minInclination = minInclination;
            this.maxInclination = maxInclination;
            this.targetBody = targetBody;

            if (title == null)
            {
                this.title = "Orbit inclination: between " + minInclination.ToString("F1") + "° and " + maxInclination.ToString("F1") + "°";
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
            node.AddValue("minInclination", minInclination);
            node.AddValue("maxInclination", maxInclination);
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minInclination = Convert.ToDouble(node.GetValue("minInclination"));
            maxInclination = Convert.ToDouble(node.GetValue("maxInclination"));
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
            if (vessel.mainBody == targetBody && vessel.situation != Vessel.Situations.LANDED)
            {
                double inclination = vessel.orbit.inclination;

                // Inclination can momentarily be in the [0.0, 360] range before KSP adjusts it
                if (inclination > 180.0)
                {
                    inclination = 360 - inclination;
                }
                
                return inclination >= minInclination && inclination <= maxInclination;
            }

            return false;
        }
    }
}
