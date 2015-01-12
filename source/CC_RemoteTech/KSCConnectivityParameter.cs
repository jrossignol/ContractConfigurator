using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using RemoteTech;
using RemoteTech.API;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    public class KSCConnectivityParameter : RemoteTechParameter
    {
        protected string title { get; set; }
        public bool hasConnectivity { get; set; }

        public KSCConnectivityParameter()
            : this(true, "")
        {
        }

        public KSCConnectivityParameter(bool hasConnectivity, string title)
            : base()
        {
            this.title = string.IsNullOrEmpty(title) ? (hasConnectivity ? "Connected to KSC" : " Not connected to KSC") : title;
            this.hasConnectivity = hasConnectivity;
        }

        protected override string GetTitle()
        {
            return title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("title", title);
            node.AddValue("hasConnectivity", hasConnectivity);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            title = node.GetValue("title");
            hasConnectivity = ConfigNodeUtil.ParseValue<bool>(node, "hasConnectivity");
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            var satellite = RTCore.Instance.Satellites[vessel.id];
            foreach (var v in RTCore.Instance.Network[satellite])
            {
                LoggingUtil.LogVerbose(this, "    Goal = " + v.Goal.Name);
                LoggingUtil.LogVerbose(this, "    Links.Count = " + v.Links.Count);
            }
            return API.HasConnectionToKSC(vessel.id) ^ !hasConnectivity;
        }
    }
}
