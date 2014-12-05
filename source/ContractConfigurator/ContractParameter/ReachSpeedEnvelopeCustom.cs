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
    public class ReachSpeedEnvelopeCustom : VesselParameter
    {
        protected string title { get; set; }
        public double minSpeed { get; set; }
        public double maxSpeed { get; set; }

        public ReachSpeedEnvelopeCustom()
            : this(0.0f, 50000.0f, null)
        {
        }

        public ReachSpeedEnvelopeCustom(double minSpeed, double maxSpeed, string title)
            : base()
        {
            this.title = title != null ? title : "Speed: Between " + minSpeed.ToString("N0") +
                " and " + maxSpeed.ToString("N0") + " m/s";
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
            CheckVessel(FlightGlobals.ActiveVessel);
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
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
            return speed >= minSpeed && speed <= maxSpeed;
        }
    }
}
