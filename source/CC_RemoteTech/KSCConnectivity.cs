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
    public class KSCConnectivity : VesselParameter
    {
        protected string title { get; set; }
        public bool hasConnectivity { get; set; }

        private float lastUpdate = 0.0f;
        private const float UPDATE_FREQUENCY = 1.00f;

        private Dictionary<string, string> nameRemap = new Dictionary<string, string>();

        public KSCConnectivity()
            : this(true, "")
        {
        }

        public KSCConnectivity(bool hasConnectivity, string title)
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

        /// <summary>
        /// Check for whether we are in a valid state to check the given vessel.  Checks if the
        /// RemoteTech logic is initialized.
        /// </summary>
        /// <param name="vessel">The vessel - ignored.</param>
        /// <returns>True only if RemoteTech is initialized.</returns>
        protected override bool CanCheckVesselMeetsCondition(Vessel vessel)
        {
            return RTCore.Instance != null;
        }

        /*
         * Whether this vessel meets the parameter condition.
         */
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            return API.HasConnectionToKSC(vessel.id) ^ !hasConnectivity;
        }
    }
}
