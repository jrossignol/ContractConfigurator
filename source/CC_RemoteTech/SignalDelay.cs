using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using ContractConfigurator;
using RemoteTech;

namespace ContractConfigurator.RemoteTech
{
    public class SignalDelay : VesselParameter
    {
        protected string title { get; set; }
        public double minSignalDelay { get; set; }
        public double maxSignalDelay { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 1.00f;

        private Dictionary<string, string> nameRemap = new Dictionary<string, string>();

        public SignalDelay()
            : this(0.0, double.MaxValue, "")
        {
        }

        public SignalDelay(double minSignalDelay, double maxSignalDelay, string title)
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

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("title", title);
            node.AddValue("minSignalDelay", minSignalDelay);
            node.AddValue("maxSignalDelay", maxSignalDelay);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            title = node.GetValue("title");
            minSignalDelay = ConfigNodeUtil.ParseValue<double>(node, "minSignalDelay");
            maxSignalDelay = ConfigNodeUtil.ParseValue<double>(node, "maxSignalDelay");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (UnityEngine.Time.fixedTime - lastUpdate > UPDATE_FREQUENCY)
            {
                lastUpdate = UnityEngine.Time.fixedTime;
                foreach (Vessel v in GetVessels())
                {
                    CheckVessel(v);
                }
            }
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            double delay = API.GetSignalDelayToKSC(vessel.id);
            return delay >= minSignalDelay && delay <= maxSignalDelay;
        }

    }
}
