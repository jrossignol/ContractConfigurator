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
     * Custom version of the stock ReachAltitudeEnvelope parameter.
     */
    public class ReachAltitudeEnvelopeCustom : VesselParameter
    {
        protected string title { get; set; }
        public float minAltitude { get; set; }
        public float maxAltitude { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        public ReachAltitudeEnvelopeCustom()
            : this(0.0f, 50000.0f, null)
        {
        }

        public ReachAltitudeEnvelopeCustom(float minAltitude, float maxAltitude, string title)
            : base()
        {
            this.title = title != null ? title : "Altitude: Between " + minAltitude.ToString("N0") +
                " and " + maxAltitude.ToString("N0") + " meters";
            this.minAltitude = minAltitude;
            this.maxAltitude = maxAltitude;
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
            node.AddValue("maxAltitude", maxAltitude);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minAltitude = (float)Convert.ToDouble(node.GetValue("minAltitude"));
            maxAltitude = (float)Convert.ToDouble(node.GetValue("maxAltitude"));
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
            return vessel.altitude >= minAltitude && vessel.altitude <= maxAltitude;
        }
    }
}
