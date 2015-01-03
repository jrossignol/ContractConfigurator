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
    /// Parameter to indicate that a vessel has connectivity with another vessel.
    /// </summary>
    public class VesselConnectivity : RemoteTechParameter
    {
        protected string title { get; set; }
        protected bool hasConnectivity { get; set; }
        protected string vesselKey { get; set; }

        public VesselConnectivity()
            : this(null)
        {
        }

        public VesselConnectivity(string vesselKey, bool hasConnectivity = true, string title = null)
            : base()
        {
            this.title = title;
            this.vesselKey = vesselKey;
            this.hasConnectivity = hasConnectivity;
        }

        protected override string GetTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                output = (hasConnectivity ? "Direct connection to:" : "No direct connection to:");
                output += ContractVesselTracker.Instance.GetDisplayName(vesselKey);
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
            node.AddValue("hasConnectivity", hasConnectivity);
            node.AddValue("vesselKey", vesselKey);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            title = node.GetValue("title");
            hasConnectivity = ConfigNodeUtil.ParseValue<bool>(node, "hasConnectivity");
            vesselKey = node.GetValue("vesselKey");
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            
            // Check vessels
            Vessel vessel2 = ContractVesselTracker.Instance.GetAssociatedVessel(vesselKey);
            if (vessel == null || vessel2 == null)
            {
                return false;
            }

            // Get satellites
            VesselSatellite sat1 = RTCore.Instance.Satellites[vessel.id];
            VesselSatellite sat2 = RTCore.Instance.Satellites[vessel2.id];
            if (sat1 == null || sat2 == null)
            {
                return false;
            }

            // Check if there is a link
            return NetworkManager.GetLink(sat1, sat2) != null;
        }
    }
}
