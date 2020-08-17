using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using KSP.Localization;
using ContractConfigurator;
using RemoteTech;
using RemoteTech.API;

namespace ContractConfigurator.RemoteTech
{
    public class SignalDelayParameter : RemoteTechParameter
    {
        public double minSignalDelay { get; set; }
        public double maxSignalDelay { get; set; }

        public SignalDelayParameter()
            : this(0.0, double.MaxValue, "")
        {
        }

        public SignalDelayParameter(double minSignalDelay, double maxSignalDelay, string title)
            : base(title)
        {
            if (string.IsNullOrEmpty(title))
            {
                if (maxSignalDelay == double.MaxValue)
                {
                    this.title = Localizer.Format("#cc.remotetech.param.SignalDelay", Localizer.Format("#cc.param.count.atLeast", minSignalDelay.ToString("N1")));
                }
                else if (minSignalDelay == 0.0)
                {
                    this.title = Localizer.Format("#cc.remotetech.param.SignalDelay", Localizer.Format("#cc.param.count.atMost", maxSignalDelay.ToString("N1")));
                }
                else
                {
                    this.title = Localizer.Format("#cc.remotetech.param.SignalDelay", Localizer.Format("#cc.param.count.between", minSignalDelay.ToString("N1"), maxSignalDelay.ToString("N1")));
                }
            }
            else
            {
                this.title = title;
            }

            this.minSignalDelay = minSignalDelay;
            this.maxSignalDelay = maxSignalDelay;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            node.AddValue("minSignalDelay", minSignalDelay);
            node.AddValue("maxSignalDelay", maxSignalDelay);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

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
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: {0}", vessel.id);
            double delay = API.GetSignalDelayToKSC(vessel.id);
            return delay >= minSignalDelay && delay <= maxSignalDelay;
        }

    }
}
