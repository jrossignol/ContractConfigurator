using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using ContractConfigurator;
using RemoteTech;
using RemoteTech.API;

namespace ContractConfigurator.RemoteTech
{
    public class SignalDelayParameter : RemoteTechParameter
    {
        protected string title { get; set; }
        public double minSignalDelay { get; set; }
        public double maxSignalDelay { get; set; }

        public SignalDelayParameter()
            : this(0.0, double.MaxValue, "")
        {
        }

        public SignalDelayParameter(double minSignalDelay, double maxSignalDelay, string title)
            : base()
        {
            if (string.IsNullOrEmpty(title))
            {
                this.title = "Signal Delay: ";

                if (maxSignalDelay == double.MaxValue)
                {
                    this.title += "At least " + minSignalDelay.ToString("N1") + " seconds";
                }
                else if (minSignalDelay == 0.0)
                {
                    this.title += "At most " + maxSignalDelay.ToString("N1") + " seconds";
                }
                else
                {
                    this.title += "Between " + minSignalDelay.ToString("N1") + " and " + maxSignalDelay.ToString("N1") + " seconds";
                }
            }
            else
            {
                this.title = title;
            }

            this.minSignalDelay = minSignalDelay;
            this.maxSignalDelay = maxSignalDelay;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            node.AddValue("title", title);
            node.AddValue("minSignalDelay", minSignalDelay);
            node.AddValue("maxSignalDelay", maxSignalDelay);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            title = node.GetValue("title");
            minSignalDelay = ConfigNodeUtil.ParseValue<double>(node, "minSignalDelay");
            maxSignalDelay = ConfigNodeUtil.ParseValue<double>(node, "maxSignalDelay");
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            double delay = API.GetSignalDelayToKSC(vessel.id);
            return delay >= minSignalDelay && delay <= maxSignalDelay;
        }

    }
}
