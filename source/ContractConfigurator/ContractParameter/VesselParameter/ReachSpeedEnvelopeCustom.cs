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
     * Custom version of the stock ReachSpeedEnvelope parameter.
     */
    [Obsolete("Obsolete, use ReachState")]
    public class ReachSpeedEnvelopeCustom : VesselParameter
    {
        protected string title { get; set; }
        public double minSpeed { get; set; }
        public double maxSpeed { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.1f;

        public ReachSpeedEnvelopeCustom()
            : this(0.0f, 50000.0f, null)
        {
        }

        public ReachSpeedEnvelopeCustom(double minSpeed, double maxSpeed, string title)
            : base()
        {
            if (title == null)
            {
                this.title = "Speed: ";

                if (maxSpeed == double.MaxValue)
                {
                    this.title += "At least " + minSpeed.ToString("N0") + " m/s";
                }
                else if (minSpeed == 0.0)
                {
                    this.title += "At most " + maxSpeed.ToString("N0") + " m/s";
                }
                else
                {
                    this.title += "Between " + minSpeed.ToString("N0") + " and " + maxSpeed.ToString("N0") + " m/s";
                }
            }
            else
            {
                this.title = title;
            }

            this.minSpeed = minSpeed;
            this.maxSpeed = maxSpeed;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("title", title);
            node.AddValue("minSpeed", minSpeed);
            node.AddValue("maxSpeed", maxSpeed);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            title = node.GetValue("title");
            minSpeed = Convert.ToDouble(node.GetValue("minSpeed"));
            maxSpeed = Convert.ToDouble(node.GetValue("maxSpeed"));
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
            double speed = GetVesselSpeed(vessel);
            return speed >= minSpeed && speed <= maxSpeed;
        }

        protected double GetVesselSpeed(Vessel vessel)
        {
            switch (vessel.situation)
            {
                case Vessel.Situations.FLYING:
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                case Vessel.Situations.SPLASHED:
                    return vessel.srfSpeed;
                default:
                    return vessel.obt_speed;
            }
        }
    }
}
