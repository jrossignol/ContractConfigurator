using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using RemoteTech;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    /// <summary>
    /// Parameter to indicate that a satellite can connect to the active vessel within a given range.
    /// </summary>
    public class ActiveVesselConnectionParameter : RemoteTechParameter
    {
        protected string title { get; set; }
        protected double range { get; set; }

        public ActiveVesselConnectionParameter()
            : this(0.0, null)
        {
        }

        public ActiveVesselConnectionParameter(double range, string title = null)
            : base()
        {
            this.title = title;
            this.range = range;
        }

        protected override string GetTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                output = "Active vessel antenna range: " + RemoteTechAssistant.RangeString(range);
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("title", title);
            node.AddValue("range", range);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            title = node.GetValue("title");
            range = ConfigNodeUtil.ParseValue<double>(node, "range");
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);

            VesselSatellite satellite = RTCore.Instance.Satellites[vessel.id];
            foreach (IAntenna a in satellite.Antennas.Where(a => a.Activated && a.Powered && (a.Omni > 0.0f || a.Target == NetworkManager.ActiveVesselGuid)))
            {
                if (a.Omni > range || a.Dish > range)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
