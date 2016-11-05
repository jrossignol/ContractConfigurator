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
    /// <summary>
    /// Parameter for checking the relay antenna power of a vessel
    /// </summary>
    public class HasAntennaRelay : VesselParameter
    {
        protected double minAntennaPower { get; set; }
        protected double maxAntennaPower { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public HasAntennaRelay()
            : this(0.0)
        {
        }

        public HasAntennaRelay(double minAntennaPower = 0.0, double maxAntennaPower = double.MaxValue, string title = null)
            : base(title)
        {
            this.minAntennaPower = minAntennaPower;
            this.maxAntennaPower = maxAntennaPower;
            if (title == null)
            {
                this.title = "Relay antenna (combined): ";

                if (maxAntennaPower == double.MaxValue)
                {
                    this.title += "At least " + minAntennaPower + " power";
                }
                else if (minAntennaPower == 0.0)
                {
                    this.title += "At most " + maxAntennaPower + " power";
                }
                else
                {
                    this.title += "Between " + minAntennaPower + " and " + maxAntennaPower + " power";
                }
            }
            else
            {
                this.title = title;
            }
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);
            node.AddValue("minAntennaPower", minAntennaPower);
            if (maxAntennaPower != double.MaxValue)
            {
                node.AddValue("maxAntennaPower", maxAntennaPower);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            minAntennaPower = Convert.ToDouble(node.GetValue("minAntennaPower"));
            maxAntennaPower = node.HasValue("maxAntennaPower") ? Convert.ToDouble(node.GetValue("maxAntennaPower")) : double.MaxValue;
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

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            double antennaPower = 0.0f;
            if (vessel.connection != null)
            {
                antennaPower = vessel.connection.Comm.antennaRelay.power;
            }
            return antennaPower >= minAntennaPower && antennaPower <= maxAntennaPower;
        }
    }
}
