using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using KSP.Localization;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking the relay or transmit antenna power of a vessel
    /// </summary>
    public class HasAntenna : VesselParameter
    {
        public enum AntennaType
		{
			RELAY,
			TRANSMIT
		};

		protected double minAntennaPower { get; set; }
        protected double maxAntennaPower { get; set; }
		protected AntennaType antennaType { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 0.25f;

        public HasAntenna()
            : this(0.0)
        {
        }

		public HasAntenna(double minAntennaPower = 0.0, double maxAntennaPower = double.MaxValue, AntennaType antennaType = AntennaType.TRANSMIT, string title = null)
            : base(title)
        {
            this.minAntennaPower = minAntennaPower;
            this.maxAntennaPower = maxAntennaPower;
			this.antennaType = antennaType;

            if (title == null)
            {
                string countStr;
                if (maxAntennaPower == double.MaxValue)
                {
                    countStr = Localizer.Format("#cc.param.count.atLeast", KSPUtil.PrintSI(minAntennaPower,""));
                }
                else if (minAntennaPower == 0.0)
                {
                    countStr = Localizer.Format("#cc.param.count.atMost", KSPUtil.PrintSI(maxAntennaPower,""));
                }
                else
                {
                    countStr = Localizer.Format("#cc.param.count.between", KSPUtil.PrintSI(minAntennaPower, ""), KSPUtil.PrintSI(maxAntennaPower, ""));
                }

                this.title = Localizer.Format(antennaType == AntennaType.TRANSMIT ? "#cc.param.HasAntenna.transmit" : "#cc.param.HasAntenna.relay", countStr);
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
			node.AddValue("antennaType", antennaType);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);
            minAntennaPower = Convert.ToDouble(node.GetValue("minAntennaPower"));
            maxAntennaPower = node.HasValue("maxAntennaPower") ? Convert.ToDouble(node.GetValue("maxAntennaPower")) : double.MaxValue;
			antennaType = ConfigNodeUtil.ParseValue<AntennaType>(node, "antennaType", AntennaType.TRANSMIT);
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
				if (antennaType == AntennaType.RELAY)
				{
					antennaPower = vessel.connection.Comm.antennaRelay.power;
				}
				else
				{
					antennaPower = vessel.connection.Comm.antennaTransmit.power;
				}
            }
            return antennaPower >= minAntennaPower && antennaPower <= maxAntennaPower;
        }
    }
}
